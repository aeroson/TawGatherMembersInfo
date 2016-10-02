using Neitri;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TawGatherMembersInfo
{
	public class RoasterManager
	{
		Thread thread;
		WebDataParser dataParser;

		[Dependency]
		DbContextProvider db;

		[Dependency]
		FileSystem fileSystem;

		[Dependency]
		Config config;

		public RoasterManager(IDependencyManager dependency)
		{
			dataParser = dependency.Create<WebDataParser>();
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

		SessionMannager sessionManager = new SessionMannager();

		void ThreadMain()
		{
			// if this is the startup then update profiles really fast
			var isFirstRun = true;
			var profileUpdateDelayMiliSeconds = 1000;

			Task gatherBasicInfoTask = null;
			gatherBasicInfoTask = Task.Run(() => GatherBasicInformationFromUnitId1Roaster());

			while (true)
			{
				gatherBasicInfoTask?.Wait();
				{
					var personsUpdated = new HashSet<string>();
					foreach (var tawUnitId in config.UnitIdsToGatherMemberProfileInfo)
					{
						Log.Trace($"parsing people from unit taw id:{tawUnitId}");

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
							if (personsUpdated.Contains(personName)) continue;
							personsUpdated.Add(personName);

							var task = Task.Run(async () =>
							{
								await dataParser.UpdateInfoFromProfilePage(sessionManager, personName);
							});
							tasks.Add(task);
						}

						Log.Trace($"parsing people from unit taw id:{tawUnitId}, all tasks started");

						Task.WaitAll(tasks.ToArray());

						Log.Trace($"parsing people from unit taw id:{tawUnitId}, done");
					}
				}

				{
					long eventIdStart;
					using (var data = db.NewContext) eventIdStart = data.Events.OrderByDescending(e => e.TawId).Take(1).Select(e => e.TawId).FirstOrDefault();
					if (eventIdStart == default(long)) eventIdStart = 65000;
					eventIdStart++;

					var doBreak = new System.Threading.ManualResetEventSlim();

					var tasks = new List<Task>();

					for (long i = 0; i < 20000; i++)
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
							Task.WaitAll(tasks.ToArray());
							tasks.Clear();
						}
					}
				}

				gatherBasicInfoTask = Task.Run(() => GatherBasicInformationFromUnitId1Roaster());

				if (isFirstRun) isFirstRun = false;
				profileUpdateDelayMiliSeconds = 60 * 1000;
			}
		}
	}
}