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
        public RoasterData CurrentData { get { return currentData; } }

        public event Action OnRoasterDataUpdated;

        volatile RoasterData currentData;
        RoasterFactory roasterFactory;
        InstancesContainer instances;
        Thread thread;

        public RoasterFactoryHandler(InstancesContainer instances)
        {
            this.instances = instances;
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
            roasterFactory.data.Save(stream);
            stream.Position = 0;
            currentData = RoasterData.Load(stream);
            if (OnRoasterDataUpdated != null) OnRoasterDataUpdated();
        }

        void ThreadMain()
        {
            roasterFactory = new RoasterFactory();

            var path = Path.Combine("data", "backup");
            roasterFactory.data = RoasterData.Load(path);
            PushDataToFront();
            roasterFactory.GatherBasicInformationFromUnitId1Roaster();
            PushDataToFront();
            roasterFactory.data.Save(path);

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
                roasterFactory.data.Save(path);

            }

        }

    }
}
