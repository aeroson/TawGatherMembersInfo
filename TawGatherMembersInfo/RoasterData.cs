using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Web;
using Neitri;

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

		public int GetNextPersonId()
		{
			return allPersons.Count;
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
				p.UnitToPositionNameShort.Clear();
				p.ClearCache();
			}
		}

		public Person GetOrCreateEmptyPerson(string name, int? id = null)
		{
			Person person;
			if (nameToPerson.TryGetValue(name, out person) == false)
			{
				person = new Person();
				person.Name = name;
				if (id.HasValue) person.Id = id.Value;
				else person.Id = GetNextPersonId();
				nameToPerson[name] = person;
				allPersons.Add(person);
			}			
			return person;
		}
		public Person GetOrUpdateOrCreatePerson(string text, Unit parentUnit)
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
					positionNameShort = Person.positionNameShortToPositionNameLong.Reverse.GetValue(positionNameLong, null);
					if (positionNameShort == null) Log.Error("cannot find positionNameShortToPositionNameLong.Reverse[" + positionNameLong + "]");
				}
			}


			var person = GetOrCreateEmptyPerson(name);
		
			person.Name = name;
			person.RankNameShort = rank;
			if (onLeave) person.Status = "on leave";

			parentUnit.personToPositionNameShort[person] = positionNameShort;
			person.UnitToPositionNameShort[parentUnit] = positionNameShort;
			allPersons.Add(person);

			return person;
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
				foreach (var p in allPersons.OrderBy((person) => person.Id))
				{
					s.WriteLine(p.Name);
				}
			}
			Log.Info("saved roaster data to '" + pathAndName + ".data.bin' and '" + pathAndName + ".personsOrder.txt'");
		}


		public static RoasterData Load(string pathAndName)
		{
			RoasterData data = null;
			var dataPath = pathAndName + ".data.bin";
			if (File.Exists(dataPath))
			{
				data = Load(File.Open(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read));
				Log.Info("loaded data from '" + dataPath + "'");
			}
			else
			{
				data = new RoasterData();
				Log.Info("'" + dataPath + "' not found, creating empty data");

				var personsOrderPath = pathAndName + ".personsOrder.txt";
				if (File.Exists(personsOrderPath))
				{
					Log.Info("found '" + personsOrderPath + "' creating name only persons to preserve IDs");
					foreach (var line in File.ReadAllLines(personsOrderPath))
					{
						data.GetOrCreateEmptyPerson(line);
					}
				}
				else
				{
					Log.Info("'" + personsOrderPath + "' not found, no persons created at all");
				}

			}
			return data;
		}
	}

}
