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
			var profileUpdateDelayMiliSeconds = 100;

			Task.Run(() => GatherBasicInformationFromUnitId1Roaster());
			Task gatherBasicInfoTask = null;

			while (true)
			{
				gatherBasicInfoTask?.Wait();
				{
					var personsUpdated = new HashSet<string>();
					foreach (var tawUnitId in config.UnitIdsToGatherMemberProfileInfo)
					{
						IEnumerable<string> peopleNames;
						using (var data = db.NewContext)
						{
							var unit = data.Units.FirstOrDefault(u => u.TawId == tawUnitId);
							if (unit == null) break;
							peopleNames = unit.GetAllPeopleNames();
						}

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

						Task.WaitAll(tasks.ToArray());
					}
				}

				{
					long eventItStart;
					using (var data = db.NewContext) eventItStart = data.Events.OrderByDescending(e => e.TawId).Take(1).Select(e => e.TawId).FirstOrDefault();
					if (eventItStart == default(long)) eventItStart = 65000;
					eventItStart++;

					var doBreak = new System.Threading.ManualResetEventSlim();

					var tasks = new List<Task>();

					for (long i = eventItStart; i < eventItStart + 1000; i++)
					{
						if (doBreak.IsSet) break;
						var task = Task.Run(async () =>
						{
							if (doBreak.IsSet) return;
							var result = await dataParser.ParseEventData(sessionManager, i);
							if (result == WebDataParser.ParseEventResult.InvalidUriProbablyLastEvent) doBreak.Set();
						});
						tasks.Add(task);
					}

					Task.WaitAll(tasks.ToArray());
				}

				gatherBasicInfoTask = Task.Run(() => GatherBasicInformationFromUnitId1Roaster());

				if (isFirstRun) isFirstRun = false;
				profileUpdateDelayMiliSeconds = 60 * 1000;
			}
		}
	}
}