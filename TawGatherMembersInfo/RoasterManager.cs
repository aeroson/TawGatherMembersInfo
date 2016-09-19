using Neitri;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TawGatherMembersInfo
{
	public class RoasterManager
	{
		LoggedInSession session;
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
			session = dependency.CreateAndRegister<LoggedInSession>();
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
		public void GatherBasicInformationFromUnitId1Roaster()
		{
			dataParser.UpdateUnitContents(1);
		}

		void ThreadMain()
		{
			//GatherBasicInformationFromUnitId1Roaster();

			long eventItStart;
			using (var data = db.NewContext) eventItStart = data.Events.OrderByDescending(e => e.TawId).Take(1).Select(e => e.TawId).FirstOrDefault();
			if (eventItStart == default(long)) eventItStart = 65000;
			eventItStart++;

			eventItStart = 70724;
			for (long i = eventItStart; i < 72067; i++)
			{
				//[2016.09.18 21:15:11.971][E] ecountered errorenous event, taw id:66583
				//[2016.09.18 21:49:46.818][E] ecountered errorenous event, taw id:67302
				//[2016.09.18 21:58:54.031][E] ecountered errorenous event, taw id:67488
				//[2016.09.18 22:17:49.893][E] ecountered errorenous event, taw id:67843
				//[2016.09.18 22:46:02.415][E] ecountered errorenous event, taw id:68411
				//[2016.09.18 22:47:43.379][E] ecountered errorenous event, taw id:68449

				dataParser.ParseEventData(i);
			}

			// if this is the startup then update profiles really fast
			var isFirstRun = true;
			var profileUpdateDelayMiliSeconds = 100;

			while (true)
			{
				session.ClearCookies();

				var personsUpdated = new HashSet<string>();
				foreach (var tawUnitId in config.UnitIdsToGatherMemberProfileInfo)
				{
					IEnumerable<string> peopleNames;
					using (var data = db.NewContext)
					{
						var unit = data.Units.FirstOrDefault(u => u.TawId == tawUnitId);
						peopleNames = unit.GetAllPeopleNames();
					}

					foreach (var personName in peopleNames)
					{
						if (personsUpdated.Contains(personName)) continue;
						personsUpdated.Add(personName);
						dataParser.UpdateInfoFromProfilePage(personName);
						//Thread.Sleep(profileUpdateDelayMiliSeconds);
					}
				}

				{
				}

				GatherBasicInformationFromUnitId1Roaster();

				if (isFirstRun) isFirstRun = false;
				profileUpdateDelayMiliSeconds = 60 * 1000;
			}
		}
	}
}