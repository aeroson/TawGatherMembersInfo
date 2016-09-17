using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using Neitri;

namespace TawGatherMembersInfo
{
	public class RoasterManager
	{
		public RoasterData FrontRoaster { get; private set; }

		RoasterData WorkingRoaster
		{
			get
			{
				return session.roaster;
			}
			set
			{
				session.roaster = value;
			}
		}

		public event Action OnRoasterDataUpdated;
		LoggedInSession session;
		Thread thread;

		[Dependency]
		FileSystem fileSystem;

		[Dependency]
		Config config;

		public void Join()
		{
			thread.Join();
		}
		public void Run()
		{
			thread = new Thread(ThreadMain);
			thread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			thread.Start();
		}

		public void Stop()
		{
			if (thread != null && thread.IsAlive) thread.Abort();
		}

		void PushDataToFront()
		{
			var stream = new System.IO.MemoryStream();
			WorkingRoaster.SaveToStream(stream);
			stream.Position = 0;
			FrontRoaster = RoasterData.LoadFromStream(stream);
			if (FrontRoaster.allUnits.Count > 0 && FrontRoaster.allPersons.Count > 0)
				OnRoasterDataUpdated?.Invoke();
		}

		void ThreadMain()
		{
			session = new LoggedInSession();

			var path = fileSystem.GetDirectory("data");
			WorkingRoaster = RoasterData.LoadFromDirectory(path);
			PushDataToFront();
			session.GatherBasicInformationFromUnitId1Roaster();
			PushDataToFront();
			WorkingRoaster.SaveToDirectory(path);

			// if this is the startup then update profiles really fast
			var isFirstRun = true;
			var profileUpdateDelayMiliSeconds = 100;

			while (true)
			{

				session.ClearCookies();
				session.GatherBasicInformationFromUnitId1Roaster();

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

				// gather events
				{
					for (int i = 0; i < 1000; i++)
					{
						session.GatherEventData(WorkingRoaster.nextEventIdToGather);
						WorkingRoaster.nextEventIdToGather++;
					}

				}



				if (isFirstRun) isFirstRun = false;
				profileUpdateDelayMiliSeconds = 60 * 1000;

				PushDataToFront();
				WorkingRoaster.SaveToDirectory(path);

			}

		}

	}
}
