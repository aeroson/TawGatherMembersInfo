using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TawGatherMembersInfo.Models
{
	[Serializable]
	public class Unit
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public virtual long Id { get; set; }

		[Index(IsUnique = true)]
		public virtual int TawId { get; set; }

		/// <summary>
		/// Possible values: Division, Batallion, Platoon, Squad, Fire Team, and more
		/// </summary>
		public virtual string Type { get; set; } = "";

		public virtual string Name { get; set; } = "noname";
		public virtual Unit ParentUnit { get; set; }
		public virtual ICollection<Unit> ChildUnits { get; set; }
		public virtual ICollection<PersonToUnit> Persons { get; set; }

		public Dictionary<Person, string> PersonToPositionNameShort => Persons.ToDictionary(p => p.Person, p => p.PositionNameShort);

		public Person HighestRankingPerson
		{
			get
			{
				Person highestPerson = null;
				int highestPriority = int.MinValue;

				foreach (var kvp in Persons)
				{
					var positionNameShort = kvp.PositionNameShort;
					var person = kvp.Person;

					var positionPriority = Person.positionNameShortTeamSpeakNamePriorityOrder.IndexOf(positionNameShort);
					if (positionPriority > highestPriority)
					{
						highestPriority = positionPriority;
						highestPerson = person;
					}
				}

				return highestPerson;
			}
		}

		/// <summary>
		/// Returns the prefix you use fot TeamSpeak name
		/// AM1 1st Battalion North American == AM 1
		/// AM2 2nd Battalion European == AM 2
		/// SOCOP = SOCOP
		/// </summary>
		public string TeamSpeakNamePrefix
		{
			get
			{
				// special use cases
				if (Type.ToLower() == "division" && Name.ToLower().Contains("arma ")) return "AM";

				var prefix = "";
				var nameParts = Name.Split(' '); // AM1 1st Battalion North American || AM2 2nd Battalion European
				prefix = nameParts[0];
				int lastCharAsInt;
				var isLastCharNumber = int.TryParse(prefix.Last().ToString(), out lastCharAsInt);
				if (isLastCharNumber)
				{
					// first part of battalion name is TS prefix
					prefix = prefix.Substring(0, prefix.Length - 1) + " " + lastCharAsInt;
				}

				// special use case
				if (prefix == "TAW") return null;

				// prefix must be uppercase, if not its not valid, then return null
				if (prefixValidRegexp.IsMatch(prefix)) return prefix;
				return null;
			}
		}

		static Regex prefixValidRegexp = new Regex("^[0-9 A-Z]*$");

		public static string GetUnitRoasterPage(int unitTawId)
		{
			if (unitTawId < 1) throw new IndexOutOfRangeException("unit id must be 1 or more");
			return @"http://taw.net/unit/" + unitTawId + "/roster.aspx";
		}

		/// <summary>
		/// Fills provided hashset with all persons from this unit and child units, recursive call.
		/// </summary>
		/// <param name="persons"></param>
		public void FillWithAllPersons(HashSet<Person> persons)
		{
			foreach (var kvp in this.Persons) persons.Add(kvp.Person);
			foreach (var unit in ChildUnits) unit.FillWithAllPersons(persons);
		}

		public HashSet<Person> GetAllPersons()
		{
			var hs = new HashSet<Person>();
			FillWithAllPersons(hs);
			return hs;
		}

		public string PrettyTreePrint(int depth = 0)
		{
			var sb = new StringBuilder();
			sb.AppendLine();
			for (int i = 1; i < depth; i++) sb.Append("|");
			sb.Append("|unit=" + Type + "=" + Name + " tawId:" + Id);
			foreach (var c in Persons)
			{
				sb.AppendLine();
				for (int i = 0; i < depth; i++) sb.Append("|");
				sb.Append("|");
				sb.Append("person=" + c.Person + "=" + c.PositionNameShort);
			}
			foreach (var c in ChildUnits)
			{
				sb.Append(c.PrettyTreePrint(depth + 1));
			}
			return sb.ToString();
		}

		public override string ToString()
		{
			return Type + " - " + Name;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Unit);
		}

		public bool Equals(Unit other)
		{
			if (other == null) return false;
			return Id == other.Id;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}