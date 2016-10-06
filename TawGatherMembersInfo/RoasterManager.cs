using Neitri;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TawGatherMembersInfo
{
	public class RoasterManager : IOnDependenciesResolved
	{
		public event Action OnDataGatheringCycleCompleted;

		Thread thread;
		WebDataParser dataParser;

		[Dependency]
		DbContextProvider db;

		[Dependency]
		FileSystem fileSystem;

		[Dependency]
		Config config;

		SessionMannager sessionManager;

		public RoasterManager(IDependencyManager dependency)
		{
			dataParser = dependency.Create<WebDataParser>();
		}

		public void OnDependenciesResolved()
		{
			sessionManager = new SessionMannager(config.MaxConcurrentWebSessions, config.MaxWebRequestsPerMinutePerSession);
		}

		public void Join()
		{
			thread.Join();
		}

		public void Run()
		{
			thread = new Thread(ThreadMain);
			thread.Priority = ThreadPriority.Highest;
			thread.Name = this.GetType().ToString();
			thread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			thread.Start();
		}

		public void Stop()
		{
			if (thread != null && thread.IsAlive) thread.Abort();
		}

		/// <summary>
		/// Gather basic information from unit roaster, still needs more detailed updating for each person from his/her profile page.
		/// </summary>
		async Task GatherBasicInformationFromUnitId1Roaster()
		{
			await dataParser.UpdateUnitContents(sessionManager, 1);
		}

		void Print(AggregateException es)
		{
			foreach (var e in es.InnerExceptions)
			{
				Log.Fatal(e);
			}
		}

		void ThreadMain()
		{
			Task gatherBasicInfoTask = null;

			while (true)
			{
				gatherBasicInfoTask = Task.Run(() => GatherBasicInformationFromUnitId1Roaster());

				try
				{
					gatherBasicInfoTask?.Wait();
				}
				catch (AggregateException e)
				{
					Print(e);
				}

				{
					var personsUpdated = new HashSet<string>();
					foreach (var tawUnitId in config.UnitsToGatherMemberInfo)
					{
						Log.Trace($"parsing people from unit taw id:{tawUnitId}, gathering people");

						HashSet<string> peopleNames;
						using (var data = db.NewContext)
						{
							var unit = data.Units.FirstOrDefault(u => u.TawId == tawUnitId);
							if (unit == null) break;
							peopleNames = unit.GetAllPeopleNames();
						}

						Log.Trace($"parsing people from unit taw id:{tawUnitId}, got {peopleNames.Count} people");

						var tasks = new List<Task>();

						foreach (var personName in peopleNames)
						{
							var personNameCopy = personName;
							if (personsUpdated.Contains(personNameCopy)) continue;
							personsUpdated.Add(personNameCopy);

							var task = Task.Run(async () =>
							{
								await dataParser.UpdateInfoFromProfilePage(sessionManager, personNameCopy);
							});
							tasks.Add(task);
						}

						Log.Trace($"parsing people from unit taw id:{tawUnitId}, all tasks started");

						try
						{
							Task.WaitAll(tasks.ToArray());
						}
						catch (AggregateException e)
						{
							Print(e);
						}

						Log.Trace($"parsing people from unit taw id:{tawUnitId}, done");
					}
				}

				{
					long eventIdStart;
					using (var data = db.NewContext) eventIdStart = data.Events.OrderByDescending(e => e.TawId).Take(1).Select(e => e.TawId).FirstOrDefault();
					if (eventIdStart == default(long)) eventIdStart = 0; // 65000 is theoretically enough, it is about 1 year back, but sometimes we want more
					eventIdStart++;

					var doBreak = new System.Threading.ManualResetEventSlim();

					var tasks = new List<Task>();

					for (long i = 0; i < 100000; i++)
					{
						long eventId = eventIdStart + i;
						if (doBreak.IsSet) break;
						var task = Task.Run(async () =>
						{
							if (doBreak.IsSet) return;
							var result = await dataParser.ParseEventData(sessionManager, eventId);
							if (result == WebDataParser.ParseEventResult.InvalidUriProbablyLastEvent)
							{
								Log.Info("found probably last event with taw id:" + eventId);
								doBreak.Set();
							}
						});
						tasks.Add(task);
						if (i % 200 == 0)
						{
							try
							{
								Task.WaitAll(tasks.ToArray());
							}
							catch (AggregateException e)
							{
								Print(e);
							}
							tasks.Clear();
						}
					}
				}

				try
				{
					OnDataGatheringCycleCompleted?.Invoke();
				}
				catch (Exception e)
				{
					Log.Fatal(e);
				}

				Log.Info($"pausing data gathering loop for {config.WebCrawlerLoopPauseSeconds} seconds");

				Thread.Sleep(config.WebCrawlerLoopPauseSeconds);
			}
		}
	}
}