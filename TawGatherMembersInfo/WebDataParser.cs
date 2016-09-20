using HtmlAgilityPack;
using Neitri;
using Neitri.WebCrawling;
using System;
using System.Linq;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class WebDataParser
	{
		[Dependency]
		LoggedInSession session;

		[Dependency]
		DbContextProvider db;

		MyDbContext data;

		public void UpdateUnitContents(int tawUnitId)
		{
			lock (this)
			{
				var url = Unit.GetUnitRoasterPage(tawUnitId);
				var response = session.GetUrl(url);
				var html = response.HtmlDocument;

				var roasterDiv = html.GetElementbyId("ctl00_bcr_UpdatePanel1").SelectSingleNode("./div/ul");

				using (data = db.NewContext)
				{
					ParseUnitContents(roasterDiv, null);
					data.SaveChanges();
				}
			}
		}

		void ParseUnitContents(HtmlNode unitNamePlusUl, Unit parentUnit)
		{
			var unitTypeNameElement = unitNamePlusUl.SelectSingleNode("li | span");
			var unitTypeA = unitTypeNameElement.SelectSingleNode("*/a[1] | a[1]");
			var unitNameA = unitTypeNameElement.SelectSingleNode("*/a[2] | a[2]");

			var type = unitTypeA.InnerText;
			var tawId = int.Parse(unitNameA.GetAttributeValue("href", "/unit/-1.aspx").TakeStringBetweenLast("/", ".aspx"));
			var name = unitNameA.InnerText;

			Log.Trace("parsing unit from units roaster, taw unit id: " + tawId);

			var unit = GetUnit(tawId, name);
			unit.Type = type;
			if (parentUnit != null) unit.ParentUnit = parentUnit;

			data.SaveChanges();

			var children = unitNamePlusUl.SelectSingleNode("ul");

			foreach (var child in children.ChildNodes)
			{
				var personA = child.SelectSingleNode("a");
				if (personA != null)
				{
					// person
					var text = child.InnerText;
					ParsePersonFromUnitRoaster(text, unit);
				}
				else
				{
					// unit
					ParseUnitContents(child, unit);
				}
			}
		}

		Person GetPersonFromName(string name)
		{
			var person = data.People.FirstOrDefault(p => p.Name == name);
			if (person == null)
			{
				person = new Person();
				person.Name = name;
				person = data.People.Add(person);
			}
			return person;
		}

		Unit GetUnit(int unitTawId, string name)
		{
			var unit = data.Units.FirstOrDefault(u => u.TawId == unitTawId);
			if (unit == null)
			{
				unit = new Unit();
				unit.TawId = unitTawId;
				unit = data.Units.Add(unit);
			}
			unit.Name = name;
			return unit;
		}

		void ParsePersonFromUnitRoaster(string text, Unit unit)
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

			var person = GetPersonFromName(name);

			person.Name = name;
			person.RankNameShort = rank;
			if (onLeave) person.Status = "on leave";

			var personToUnit = data.PeopleToUnits.FirstOrDefault(p => p.PersonId == person.PersonId && p.UnitId == unit.UnitId);
			if (personToUnit == null)
			{
				personToUnit = new PersonToUnit();
				personToUnit.Person = person;
				personToUnit.Unit = unit;
				personToUnit = data.PeopleToUnits.Add(personToUnit);
			}
			personToUnit.PositionNameShort = positionNameShort;

			data.SaveChanges();
		}

		public void UpdateInfoFromProfilePage(string personName)
		{
			lock (this)
			{
				Log.Trace("updating profile for " + personName + " start");

				var url = Person.GetPersonProfilePageUrl(personName);
				var response = session.GetUrl(url);
				var html = response.HtmlDocument;

				Log.Trace("updating profile for " + personName + " got web response");

				using (data = db.NewContext)
				{
					var person = data.People.FirstOrDefault(p => p.Name == personName);
					if (person == null)
					{
						Log.Error("person with name " + personName + " was not found in database");
						return;
					}

					// steam profile id
					var steamProfileLinkPrefix = "http://steamcommunity.com/profiles/";
					var steamProfileLinkElement = html.GetElementbyId("hfSteam");
					if (steamProfileLinkElement != null)
					{
						var steamProfileLink = steamProfileLinkElement.GetAttributeValue("href", steamProfileLinkPrefix + "-1");
						var steamId = long.Parse(steamProfileLink.Substring(steamProfileLinkPrefix.Length));
						person.SteamId = steamId;
					}

					// avatar image
					var avatarElement = html.DocumentNode.SelectSingleNode("//*[@class='dossieravatar']/img");
					if (avatarElement != null)
					{
						var avatarImageLink = avatarElement.GetAttributeValue("src", null);
						if (avatarImageLink != null)
						{
							person.AvatarImageUrl = "http://taw.net" + avatarImageLink;
						}
					}

					// bio
					var biographyElement = html.DocumentNode.SelectSingleNode("//*[@id='dossierbio']");
					if (biographyElement != null)
					{
						var biography = biographyElement.InnerText.Trim();
						var bioTextHeader = "Bio:";
						if (biography.StartsWith(bioTextHeader)) biography = biography.Substring(bioTextHeader.Length);
						person.BiographyContents = biography;
					}

					var table = new HtmlTwoColsStringTable(html.DocumentNode.SelectNodes("//*[@class='dossiernexttopicture']/table//tr"));

					// country
					person.CountryName = table.GetValue("Location:", person.CountryName);
					person.Status = table.GetValue("Status:", person.Status).ToLower();
					{
						var joined = table.GetValue("Joined:", "01-01-0001"); // 10-03-2014  month-day-year // wtf.. americans...
						var joinedParts = joined.Split('-');
						person.DateJoinedTaw = new DateTime(
							int.Parse(joinedParts[2]),
							int.Parse(joinedParts[0]),
							int.Parse(joinedParts[1])
						);
					}

					person.LastProfileDataUpdatedDate = DateTime.UtcNow;
					person.ClearCache();

					data.SaveChanges();
				}

				Log.Trace("updating profile for " + personName + " parsed and saved");
			}
		}

		DateTime ParseUsTime([NotNull] string str)
		{
			//return DateTime.ParseExact(str, "M/d/yyyy hh:mm:ss zzz", CultureInfo.InvariantCulture);
			return DateTime.Parse(str);
		}

		public enum ParseEventResult
		{
			ValidEvent,
			ErrorenousEvent,
			BaseEvent,
			InvalidUriProbablyLastEvent,
		}

		public ParseEventResult ParseEventData(long eventTawId)
		{
			lock (this)
			{
				try
				{
					Log.Trace("parsing event data, taw id:" + eventTawId + " start");
					var url = Event.GetEventPage(eventTawId);
					var response = session.GetUrl(url);
					ParseEventResult result;
					Log.Trace("parsing event data, taw id:" + eventTawId + " got web response");
					using (data = db.NewContext)
					{
						result = ParseEventData_1(response);
						data.SaveChanges();
					}
					Log.Trace("parsing event data, taw id:" + eventTawId + " parsed and saved");
					return result;
				}
				catch (Exception e)
				{
					Log.Error("ecountered errorenous event, taw id:" + eventTawId);
					Log.Error(e);
					return ParseEventResult.ErrorenousEvent;
				}
			}
		}

		ParseEventResult ParseEventData_1(MyHttpWebResponse response)
		{
			var uriPath = response.ResponseUri.AbsolutePath;
			if (uriPath.Contains("event") == false)
			{
				return ParseEventResult.InvalidUriProbablyLastEvent;
				Log.Error("the event you are trying to parse has invalid uri");
			}

			var eventTawIdStr = uriPath.Split('/', '\\').Last().RemoveFromEnd(".aspx".Length);
			var eventTawId = int.Parse(eventTawIdStr);

			var htmlText = response.ResponseText;
			htmlText = htmlText?.TakeStringAfter("ctl00_ctl00_bcr_bcr_UpdatePanel\">");
			if (htmlText.Contains("This is a Base Event and should never be seen"))
			{
				Log.Trace("event " + eventTawId + " is invalid 'base event', skipping");
				return ParseEventResult.BaseEvent; // http://taw.net/event/65132.aspx
			}

			var evt = data.Events.FirstOrDefault(e => e.TawId == eventTawId);
			if (evt == null)
			{
				evt = new Event();
				evt.TawId = eventTawId;
				evt = data.Events.Add(evt);
			}
			ParseEventData_2(evt, htmlText);
			return ParseEventResult.ValidEvent;
		}

		void ParseEventData_2(Event evt, string htmlText)
		{
			// this page is so badly coded the HTML is invalid, chrome shows it correctly though, kudos to it
			// but HtmlAgilityPack just fails on it

			var eventInfoText = htmlText.TakeStringBetween("<table cellpadding=\"20\" cellspacing=\"5\">", "</table>");

			var eventInfoDoc = new HtmlDocument();
			eventInfoDoc.LoadHtml(eventInfoText);
			var eventInfo = new HtmlTwoColsStringTable(eventInfoDoc.DocumentNode);
			/*
			Name	GRAW Practice -- Saber Squad Thursday Night (NA-SA)
			Description	GRAW Practice -- Saber Squad Thursday Night (NA-SA)
			Type	Practice
			Unit	Ghost Recon
			When	From: 6/3/2016 04:00:00 +02:00 to: 6/3/2016 05:00:00 +02:00
			Mandatory	Yes
			Cancelled	No
			*/
			evt.Name = eventInfo["Name"];
			evt.Description = eventInfo["Description"];
			evt.Type = eventInfo["Type"];
			evt.Mandatory = eventInfo["Mandatory"] == "Yes";
			evt.Cancelled = eventInfo["Cancelled"] == "Yes";

			var when = eventInfo["When"];

			var strFrom = when.TakeStringBetween("from:", "to:", StringComparison.InvariantCultureIgnoreCase).Trim();
			if (strFrom != null) evt.From = ParseUsTime(strFrom);

			var strTo = when.TakeStringAfter("to:", StringComparison.InvariantCultureIgnoreCase).Trim();
			if (strTo != null) evt.To = ParseUsTime(strTo);

			var attendeesText = htmlText.TakeStringBetween("<table width=100%>", "</table>");
			var attendessDoc = new HtmlDocument();
			attendessDoc.LoadHtml(attendeesText);
			var attendeesTable = new HtmlTable(attendessDoc.DocumentNode);

			/*
			if (evt.Attended?.Count > 0)
			{
				foreach (var attended in evt.Attended) attended.Person.Attended.Remove(attended);
				evt.Attended.Clear();
			}
			*/

			foreach (var row in attendeesTable)
			{
				var name = row[0]?.InnerText?.Trim();
				var nameHref = row[0]?.SelectSingleNode("a")?.GetAttributeValue("href", ""); // http://taw.net/event/66327.aspx last row, unit name has no link
				if (nameHref != null && nameHref.StartsWith("/member"))
				{
					var person = GetPersonFromName(name);

					var personToEvent = data.PeopleToEvents.FirstOrDefault(p => p.EventId == evt.EventId && p.PersonId == person.PersonId);
					if (personToEvent == null)
					{
						personToEvent = new PersonToEvent();
						personToEvent.Event = evt;
						personToEvent.Person = person;
						personToEvent = data.PeopleToEvents.Add(personToEvent);
					}

					var attendanceStr = row[1]?.InnerText?.Trim();
					AttendanceType attendanceType = AttendanceType.Unknown;
					if (attendanceStr != null && Enum.TryParse(attendanceStr.ToLowerInvariant(), true, out attendanceType)) personToEvent.AttendanceType = attendanceType;

					var timestampStr = row[2]?.InnerText?.Trim();
					DateTime timestamp;
					//if (DateTime.TryParseExact(timestampStr, "MM-dd-yyyy hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp)) attended.timeStamp = timestamp;
					if (DateTime.TryParse(timestampStr, out timestamp)) personToEvent.TimeStamp = timestamp;
				}
				else if (nameHref != null && nameHref.StartsWith("/unit"))
				{
					var unitTawIdStr = nameHref.Split('/', '\\').Last().RemoveFromEnd(".aspx".Length);
					var unitTawId = int.Parse(unitTawIdStr);
					var unit = GetUnit(unitTawId, name);
					evt.Units.Add(unit);
				}
				else if (nameHref == null)
				{
					// event with no unit
				}
				else
				{
					throw new Exception("something is wrong, found unexpected data");
				}
			}
		}
	}
}