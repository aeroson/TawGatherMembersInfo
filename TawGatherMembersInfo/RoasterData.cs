using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Web;
using System.IO.Compression;
using Neitri;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
    /*
	[Serializable]
	public class RoasterData
	{
		public Unit rootUnit;
		public Dictionary<int, Unit> idToUnit = new Dictionary<int, Unit>();
		public HashSet<Unit> allUnits = new HashSet<Unit>();
		public HashSet<Person> allPersons = new HashSet<Person>();
		public Dictionary<string, Person> nameToPerson = new Dictionary<string, Person>();
		public HashSet<Event> allEvents = new HashSet<Event>();
		public Dictionary<int, Event> idToEvent = new Dictionary<int, Event>();

        // 70038 = 08-13-2016 21:30
        // 72065 = 09-13-2016 20:30
		public int nextEventIdToGather = 65000; // this 65000 is guaranteed to be over 60 days old

        public const string dataFileName = "backup.data.bin.gzip";
		public const string personsOrderFileName = "backup.personsOrder.txt";

		public int GetNextPersonId()
		{
			return allPersons.Count;
		}

		public Unit GetUnitByTawId(long tawId)
		{
			return idToUnit.GetValue(tawId);
		}
        public Unit GetOrCreateUnit(long tawId, string name)
        {
            var unit = GetUnitByTawId(tawId);
            if(unit == null)
            {
                unit = new Unit();
                unit.Name = name;
                unit.Id = tawId;

                allUnits.Add(unit);
                idToUnit[unit.Id] = unit;
            }
            return unit;
        }

		public Unit CreateUnit(Unit parentUnit, string text)
		{
			var parts = text.Split('-');
			var unit = new Unit();
			unit.Type = parts[0].Trim();
			if (parts.Length == 1) unit.Name = unit.Type;
			else unit.Name = parts[1].Trim();
			unit.ParentUnit = parentUnit;
			allUnits.Add(unit);
			if (unit.ParentUnit != null) unit.ParentUnit.ChildUnits.Add(unit);
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

		public Person GetOrCreateEmptyPersonFromName(string name, int? id = null)
		{
            name = name?.Trim();
            if (name == null) return null;
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
		
		public void SaveToStream(Stream stream)
		{
			var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			serializer.Serialize(stream, this);
		}

		public static RoasterData LoadFromStream(Stream stream)
		{
			var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			var data = serializer.Deserialize(stream) as RoasterData;
			return data;
		}


		public void SaveToDirectory(DirectoryPath path)
		{
			Log.Enter();

			var dataBin = path.GetFile(dataFileName);
			var personsOrder = path.GetFile(personsOrderFileName);

			using (var fileStream = File.Open(dataBin, FileMode.Create, FileAccess.Write, FileShare.Write))
			using (var zipStream = new GZipStream(fileStream, CompressionMode.Compress))
			{
				SaveToStream(zipStream);
			}

			using (var s = new StreamWriter(File.Open(personsOrder, FileMode.Create, FileAccess.Write, FileShare.Write)))
			{
				foreach (var p in allPersons.OrderBy((person) => person.Id))
				{
					s.WriteLine(p.Name);
				}
			}
			Log.Info("saved roaster data to '" + dataBin + "' and '" + personsOrder + ".personsOrder.txt'");
			Log.Exit();
		}


		public static RoasterData LoadFromDirectory(DirectoryPath path)
		{
			Log.Enter();

			var dataBin = path.GetFile(dataFileName);
			var personsOrder = path.GetFile(personsOrderFileName);

			RoasterData data = null;
			if (dataBin.Exists)
			{
				using (var fileStream = File.Open(dataBin, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (var zipStream = new GZipStream(fileStream, CompressionMode.Decompress))
				{
					data = LoadFromStream(zipStream);
				}
				Log.Info("loaded data from '" + dataBin + "'");
			}
			else
			{
				data = new RoasterData();
				Log.Info("'" + dataBin + "' not found, creating empty data");
				if (personsOrder.Exists)
				{
					Log.Info("found '" + personsOrder + "' creating name only persons to preserve IDs");
					foreach (var line in File.ReadAllLines(personsOrder))
					{
						data.GetOrCreateEmptyPersonFromName(line);
					}
				}
				else
				{
					Log.Info("'" + personsOrder + "' not found, no persons created at all");
				}

			}

			Log.Exit();
			return data;
		}
	}
    */

}
