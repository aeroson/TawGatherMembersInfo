using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace TawGatherMembersInfo
{
	[Serializable]
	public class Unit
	{
		/// <summary>
		/// Possible values: Division, Batallion, Platoon, Squad, Fire Team, and more
		/// </summary>
		public string type = "";
		public string name = "noname";
		public int id = -1;
		public Unit parentUnit;
		public HashSet<Unit> childUnits = new HashSet<Unit>();
		public Dictionary<Person, string> personToPositionNameShort = new Dictionary<Person, string>();

		public Person HighestRankingPerson
		{
			get
			{
				Person highestPerson = null;
				int highestPriority = int.MinValue;

				foreach (var kvp in personToPositionNameShort)
				{
					var positionNameShort = kvp.Value;
					var person = kvp.Key;

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
				if (type.ToLower() == "division" && name.ToLower().Contains("arma ")) return "AM";

				var prefix = "";
				var nameParts = name.Split(' '); // AM1 1st Battalion North American || AM2 2nd Battalion European
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

		public static string GetUnitRoasterPage(int unitId)
		{
			if (unitId < 1) throw new IndexOutOfRangeException("unit id must be 1 or more");
			return @"http://taw.net/unit/" + unitId + "/roster.aspx";
		}

		public void ParseUnitContents(LoggedInSession roasterFactory, HtmlNode htmlNode)
		{
			id = int.Parse(htmlNode.GetAttributeValue("id", "u-1").Substring(1));
			roasterFactory.roaster.idToUnit[id] = this;

			foreach (var child in htmlNode.ChildNodes)
			{

				var span = child.SelectSingleNode(child.XPath + "/span"); // contains the name of this unit
				var ul = child.SelectSingleNode(child.XPath + "/ul"); // list that contains all child units or persons

				if (span == null && ul == null)
				{
					// person
					var name = child.InnerText;
					var person = roasterFactory.roaster.GetOrUpdateOrCreatePersonFromUnitPage(name, this);
				}
				else
				{
					// unit
					var name = span.InnerText;
					var childUnit = roasterFactory.roaster.CreateUnit(this, name);
					childUnit.ParseUnitContents(roasterFactory, ul);
				}
			}

		}

		/// <summary>
		/// Fills provided hashset with all persons from this unit and child units, recursive call.
		/// </summary>
		/// <param name="persons"></param>
		public void FillWithAllPersons(HashSet<Person> persons)
		{
			foreach (var kvp in personToPositionNameShort) persons.Add(kvp.Key);
			foreach (var unit in childUnits) unit.FillWithAllPersons(persons);
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
			sb.Append("|unit=" + type + "=" + name + " tawId:" + id);
			foreach (var c in personToPositionNameShort)
			{
				sb.AppendLine();
				for (int i = 0; i < depth; i++) sb.Append("|");
				sb.Append("|");
				sb.Append("person=" + c.Value + "=" + c.Key);
			}
			foreach (var c in childUnits)
			{
				sb.Append(c.PrettyTreePrint(depth + 1));
			}
			return sb.ToString();
		}

		public override string ToString()
		{
			return type + " - " + name;
		}
        public override bool Equals(object obj)
        {
            return Equals(obj as Unit);
        }

        public bool Equals(Unit other)
		{
			if (other == null) return false;
            return id == other.id;
		}

		public override int GetHashCode()
		{
			return id.GetHashCode();
		}

	}

}
