using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Web;

namespace TawGatherMembersInfo
{

    [Serializable]
    public class RoasterData
    {
        public Unit rootUnit;
        public Dictionary<int, Unit> idToUnit = new Dictionary<int, Unit>();
        public HashSet<Unit> allUnits = new HashSet<Unit>();
        public HashSet<Person> allPersons = new HashSet<Person>();
        public Dictionary<string, Person> nameToPerson = new Dictionary<string, Person>();
        public int nextPersonId;

        public int GetNextPersonId()
        {
            var id = nextPersonId;
            nextPersonId++;
            return id;
        }

        public Unit CreateUnit(Unit parentUnit, string text)
        {
            var parts = text.Split('-');
            var unit = new Unit();
            unit.type = parts[0].Trim();
            if (parts.Length == 1) unit.name = unit.type;
            else unit.name = parts[1].Trim();
            unit.parentUnit = parentUnit;
            allUnits.Add(unit);
            if (unit.parentUnit != null) unit.parentUnit.childUnits.Add(unit);
            return unit;
        }

        public void ClearUnitToPersonRelations()
        {
            foreach (var u in allUnits)
            {
                u.personToPositionNameShort.Clear();
            }
            foreach (var p in allPersons)
            {
                p.unitToPositionNameShort.Clear();
                p.ClearCache();
            }
        }

        public Person GetOrCreatePerson(string text, Unit parentUnit)
        {
            /*
                text use cases:
                Commander-in-Chief - DOC, GEN5
                Commanding Officer - Constance, CPT
                Executive Officer - Deceded, LTC
                BetaHook, PFC - On Leave
                Guthrie, PFC - On Leave
                Constance, CPT
                Juvenis, COL
            */

            string name = "unnamed";
            string rank = "unranked";
            string positionNameLong = "";
            string positionNameShort = "";
            bool onLeave = false;

            {
                var dashIndex = text.LastIndexOf("-");

                if (dashIndex != -1)
                {
                    var part1 = text.Substring(0, dashIndex - 1).Trim();
                    var part2 = text.Substring(dashIndex + 1).Trim();

                    if (part2.ToLower().Contains("on leave"))
                    {
                        onLeave = true;
                        var parts = part1.Split(',');
                        name = parts[0].Trim();
                        rank = parts[1].Trim();
                    }
                    else
                    {
                        positionNameLong = part1;
                        var parts = part2.Split(',');
                        name = parts[0].Trim();
                        rank = parts[1].Trim();
                    }
                }
                else
                {
                    var parts = text.Split(',');
                    name = parts[0].Trim();
                    rank = parts[1].Trim();
                }

                if (positionNameLong != "")
                {
                    positionNameShort = Person.positionNameShortToPositionNameLong.Reverse.Get(positionNameLong, null);
                    if (positionNameShort == null) Console.WriteLine("ERROR: cannot find positionNameShortToPositionNameLong.Reverse[" + positionNameLong + "]");
                }
            }


            Person person;
            if (nameToPerson.ContainsKey(name)) person = nameToPerson[name];
            else
            {
                person = new Person();
                person.name = name;
                person.id = GetNextPersonId();
                person.rankNameShort = rank;
                if (onLeave) person.status = "on leave";
                nameToPerson[name] = person;
            }

            parentUnit.personToPositionNameShort[person] = positionNameShort;
            person.unitToPositionNameShort[parentUnit] = positionNameShort;
            allPersons.Add(person);

            return person;
        }



        public void ClearAll()
        {

            foreach (var unit in allUnits)
            {
                unit.personToPositionNameShort.Clear();
                unit.childUnits.Clear();
                unit.parentUnit = null;
            }
            allUnits.Clear();

            foreach (var person in allPersons)
            {
                person.unitToPositionNameShort.Clear();
            }
            allPersons.Clear();
        }

        public void Save(Stream stream)
        {
            var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            serializer.Serialize(stream, this);
        }

        public static RoasterData Load(Stream stream)
        {
            var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var data = serializer.Deserialize(stream) as RoasterData;
            return data;
        }


        public void Save(string pathAndName)
        {
            Save(File.Open(pathAndName + ".data.bin", FileMode.Create, FileAccess.Write, FileShare.Write));

            using (var s = new StreamWriter(File.Open(pathAndName + ".personsOrder.txt", FileMode.Create, FileAccess.Write, FileShare.Write)))
            {
                foreach (var p in allPersons.OrderBy((person) => person.id))
                {
                    s.WriteLine(p.name);
                }
            }
            Console.WriteLine("saved data to '" + pathAndName + "'");
        }


        public static RoasterData Load(string pathAndName)
        {
            var dataPath = pathAndName + ".data.bin";
            if (File.Exists(dataPath))
            {
                var data = Load(File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read));
                Console.WriteLine("loaded data from '" + pathAndName + "'");
                return data;
            }
            Console.WriteLine("'" + dataPath + "' not found, creating empty data");
            return new RoasterData();
        }
    }

}
