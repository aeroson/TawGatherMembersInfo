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
		public RoasterData CurrentRoaster { get; private set; }

		public event Action OnRoasterDataUpdated;
		LoggedInSession roasterFactory;
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
			roasterFactory.roaster.SaveToStream(stream);
			stream.Position = 0;
			CurrentRoaster = RoasterData.LoadFromStream(stream);
			if (CurrentRoaster.allUnits.Count > 0 && CurrentRoaster.allPersons.Count > 0)
				OnRoasterDataUpdated?.Invoke();
		}

		void ThreadMain()
		{
			roasterFactory = new LoggedInSession();

			var path = fileSystem.GetDirectory("data");
			roasterFactory.roaster = RoasterData.LoadFromDirectory(path);
			PushDataToFront();
			roasterFactory.GatherBasicInformationFromUnitId1Roaster();
			PushDataToFront();
			roasterFactory.roaster.SaveToDirectory(path);

			// if this is the startup then update profiles really fast
			var isFirstRun = true;
			var profileUpdateDelayMiliSeconds = 100;

			while (true)
			{

				roasterFactory.ClearCookies();
				roasterFactory.GatherBasicInformationFromUnitId1Roaster();

				var personsUpdated = new HashSet<Person>();
				var unitsIds = config.UnitIdsToGatherMemberProfileInfo;
				foreach (var unitId in unitsIds)
				{
					var unit = roasterFactory.roaster.idToUnit.GetValue(unitId, null);
					if (unit == null) continue;
					foreach (var person in unit.GetAllPersons())
					{
						if (personsUpdated.Contains(person)) continue;
						personsUpdated.Add(person);
						person.UpdateInfoFromProfilePage(roasterFactory);
						Thread.Sleep(profileUpdateDelayMiliSeconds);
					}
				}

				if (isFirstRun) isFirstRun = false;
				profileUpdateDelayMiliSeconds = 60 * 1000;

				PushDataToFront();
				roasterFactory.roaster.SaveToDirectory(path);

			}

		}

	}
}
