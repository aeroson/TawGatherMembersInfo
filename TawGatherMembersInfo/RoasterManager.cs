using Neitri;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

		ILogEnd Log => Program.Log;

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

		void BackupPeopleOrder()
		{
			var file = fileSystem.GetFile("backup.personsOrder.txt");
			using (var data = db.NewContext)
			{
				var people = data.People.OrderBy(p => p.PersonId).Select(p => p.Name).ToArray();
				File.WriteAllLines(file, people);
			}
		}

		void UpdateProfiles()
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

		void GatherEvents()
		{
			long eventIdStart;
			using (var data = db.NewContext) eventIdStart = data.Events.OrderByDescending(e => e.TawId).Take(1).Select(e => e.TawId).FirstOrDefault();
			if (eventIdStart == default(long)) eventIdStart = 110; // 65000 is theoretically enough, it is about 1 year back, but sometimes we want more
			eventIdStart++;

			var doBreak = new System.Threading.ManualResetEventSlim();

			var tasks = new List<Task>();

			for (long i = 0; i < 100000; i++)
			{
				long eventId = eventIdStart + i;

				if (eventId >= 32628 && eventId <= 33626) continue; // missing events, hard to tell if its last event or just missing one
				if (eventId >= 38804 && eventId <= 39801) continue; // again missing fucking event

				// TODO:
				// a clever algorithm that works on ranges, e.g: try eventId+2  eventId+4 .. eventId+1024,
				// then eventId+1024-2 eventId+1024-4 eventId+1024-128
				// find the next event that works by checking suitable ranges

				if (doBreak.IsSet) break;
				var task = Task.Run(async () =>
				{
					if (doBreak.IsSet) return;
					var result = await dataParser.ParseEventData(sessionManager, eventId);
					if (result == WebDataParser.ParseEventResult.InvalidUriShouldRetry)
					{
						await Task.Delay(500);
						if (doBreak.IsSet) return;
						result = await dataParser.ParseEventData(sessionManager, eventId);
						if (result == WebDataParser.ParseEventResult.InvalidUriShouldRetry)
						{
							await Task.Delay(500);
							if (doBreak.IsSet) return;
							result = await dataParser.ParseEventData(sessionManager, eventId);
							if (result == WebDataParser.ParseEventResult.InvalidUriShouldRetry)
							{
								Log.Fatal("retried to parse event taw id:" + eventId + " already 3 times, failed all of them, probably last event, stopping event parsing");
								doBreak.Set();
							}
						}
					}
				});
				tasks.Add(task);
				if (i % 10 == 0)
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

		void Print(Exception e)
		{
			Log.Fatal(e);
			var ae = e as AggregateException;
			if (ae != null) foreach (var _e in ae.InnerExceptions) Print(_e);
		}

		void Run(Action action)
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				Print(e);
			}
		}

		void ThreadMain()
		{
			while (true)
			{
				Run(() => Task.Run(() => GatherBasicInformationFromUnitId1Roaster()).Wait());

				Run(() => BackupPeopleOrder());

				Run(() => UpdateProfiles());

				Run(() => GatherEvents());

				Run(() => OnDataGatheringCycleCompleted?.Invoke());

				Log.Info($"pausing data gathering loop for {config.WebCrawlerLoopPauseSeconds} seconds");

				Thread.Sleep(config.WebCrawlerLoopPauseSeconds);
			}
		}
	}
}