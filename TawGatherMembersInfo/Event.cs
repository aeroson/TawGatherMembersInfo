using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TawGatherMembersInfo
{
	// http://taw.net/member/aeroson/events/all.aspx
	public class Event
	{
		public int tawId;
		public string name;
		public string description;
		public string type;
		public string unityName;
		public Unit unit;
		public bool mandatory;
		public bool cancelled;
		public DateTime from;
		public DateTime to;
		public Person takenBy; // attendance taken by

		public class Attended
		{
			public Person person;
			public AttendanceType attendanceType;

		}
		public List<Attended> attended = new List<Attended>();


		public static string GetEventPage(int eventTawId)
		{
			return @"http://taw.net/event/" + eventTawId + ".aspx";
		}

		// under TS AL rank can see only his own events
		public static string GetAllMemberEventsPage(string memberName)
		{
			return @"http://taw.net/member/" + memberName + "/events/all.aspx";
		}

		public void ParseEventData(LoggedInSession roasterFactory, HtmlDocument htmlDocument)
		{

			var div = htmlDocument.GetElementbyId("ctl00_ctl00_bcr_bcr_UpdatePanel");

			var blank = div.SelectNodes("tabble[1]");
			var eventInfo = div.SelectNodes("tabble[2]");
			var attendees = div.SelectNodes("tabble[3]");

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
					var person = roasterFactory.roaster.GetOrUpdateOrCreatePerson(name, this);
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
	}

	public enum AttendanceType
	{
		Attended,
		Missed, // == AWOL
		Excused,
	}
}
