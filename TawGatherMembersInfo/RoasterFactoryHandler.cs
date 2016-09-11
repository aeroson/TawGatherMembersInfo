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
	public class RoasterFactoryHandler
	{
		public RoasterData CurrentData { get; private set; }

		public event Action OnRoasterDataUpdated;
		RoasterFactory roasterFactory;
		InstancesContainer instances;
		Thread thread;

		[Dependency]
		FileSystem fileSystem = new FileSystem();

		public RoasterFactoryHandler(InstancesContainer instances)
		{
			this.instances = instances;
		}
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
			roasterFactory.data.SaveToStream(stream);
			stream.Position = 0;
			CurrentData = RoasterData.LoadFromStream(stream);
			if (CurrentData.allUnits.Count > 0 && CurrentData.allPersons.Count > 0)
				OnRoasterDataUpdated?.Invoke();
		}

		void ThreadMain()
		{
			roasterFactory = new RoasterFactory();

			var path = fileSystem.GetDirectory("data");
			roasterFactory.data = RoasterData.LoadFromDirectory(path);
			PushDataToFront();
			roasterFactory.GatherBasicInformationFromUnitId1Roaster();
			PushDataToFront();
			roasterFactory.data.SaveToDirectory(path);

			// if this is the startup then update profiles really fast
			var isFirstRun = true;
			var profileUpdateDelayMiliSeconds = 100;

			while (true)
			{

				roasterFactory.ClearCookies();
				roasterFactory.GatherBasicInformationFromUnitId1Roaster();

				var personsUpdated = new HashSet<Person>();
				var unitsIds = instances.config.Root.Descendants("unitsToGatherMemberInfo").First().Elements().Select(e => int.Parse(e.Value));
				foreach (var unitId in unitsIds)
				{
					var unit = roasterFactory.data.idToUnit.GetValue(unitId, null);
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
				roasterFactory.data.SaveToDirectory(path);

			}

		}

	}
}
