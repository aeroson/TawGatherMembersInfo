using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace TawGatherMembersInfo
{
    [Serializable]
    public class Unit
    {
        /// <summary>
        /// Possible values: Division, Batallion, Platoon, Squad, Fire Team, and more
        /// </summary>
        public string type;
        public string name;
        public int id;
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

        public static string GetUnitRoasterPage(int unitId)
        {
            if (unitId < 1) throw new IndexOutOfRangeException("unit id must be 1 or more");
            return @"http://taw.net/unit/" + unitId + "/roster.aspx";
        }

        public void ParseUnitContents(RoasterFactory roasterFactory, HtmlNode htmlNode)
        {
            id = int.Parse(htmlNode.GetAttributeValue("id", "u-1").Substring(1));
            roasterFactory.data.idToUnit[id] = this;

            foreach (var child in htmlNode.ChildNodes)
            {

                var span = child.SelectSingleNode(child.XPath + "/span"); // contains the name of this unit
                var ul = child.SelectSingleNode(child.XPath + "/ul"); // list that contains all child units or persons

                if (span == null && ul == null)
                {
                    // person
                    var name = child.InnerText;
                    var person = roasterFactory.data.GetOrCreatePerson(name, this);
                }
                else
                {
                    // unit
                    var name = span.InnerText;
                    var childUnit = roasterFactory.data.CreateUnit(this, name);
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

        public bool Equals(Unit other)
        {
            if (other == null) return false;
            return name == other.name && type == other.type;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() ^ type.GetHashCode();
        }

    }

}
