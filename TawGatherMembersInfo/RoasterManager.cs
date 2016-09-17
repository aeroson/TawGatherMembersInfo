using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using Neitri;
using TawGatherMembersInfo.Models;

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
			//var path = fileSystem.GetDirectory("data");
			//WorkingRoaster = RoasterData.LoadFromDirectory(path);
			//PushDataToFront();
			GatherBasicInformationFromUnitId1Roaster();

			//var allRanks = WorkingRoaster.allPersons.Select(p => p.RankNameShort).Distinct().ToArray();

			// if this is the startup then update profiles really fast
			var isFirstRun = true;
			var profileUpdateDelayMiliSeconds = 100;

			while (true)
			{

				session.ClearCookies();
				GatherBasicInformationFromUnitId1Roaster();

				/*
				var personsUpdated = new HashSet<Person>();
				foreach (var unitId in config.UnitIdsToGatherMemberProfileInfo)
				{
					var unit = WorkingRoaster.idToUnit.GetValue(unitId, null);
					if (unit == null) continue;
					foreach (var person in unit.GetAllPersons())
					{
						if (personsUpdated.Contains(person)) continue;
						personsUpdated.Add(person);
						person.UpdateInfoFromProfilePage(session);
						Thread.Sleep(profileUpdateDelayMiliSeconds);
					}
				}
				*/

				/*
                foreach(var person in WorkingRoaster.allPersons)
                {
                    person.UpdateInfoFromProfilePage(session);
                }
                var b = WorkingRoaster.allPersons.Select(p => p.Status).ToArray();
                */
				// gather events

				{
					for (int i = 0; i < 1000; i++)
					{
						//dataParser.ParseEventData(WorkingRoaster.nextEventIdToGather);
						//WorkingRoaster.nextEventIdToGather++;
					}

				}



				if (isFirstRun) isFirstRun = false;
				profileUpdateDelayMiliSeconds = 60 * 1000;

			}

		}

	}
}
