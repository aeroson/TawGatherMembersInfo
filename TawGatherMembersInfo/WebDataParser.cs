using HtmlAgilityPack;
using Neitri;
using Neitri.WebCrawling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            var unit = GetUnit(tawId, name);
            unit.Type = type;

            if (parentUnit != null) unit.ParentUnit = parentUnit;

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
            var person = data.Persons.FirstOrDefault(p => p.Name == name);
            if (person == null)
            {
                person = new Person();
                person.Name = name;
                person = data.Persons.Add(person);
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

            var personToUnit = data.PersonsToUnits.FirstOrDefault(p => p.Person == person && p.Unit == unit);
            if (personToUnit == null)
            {
                personToUnit = new PersonToUnit();
                personToUnit.Person = person;
                personToUnit.Unit = unit;
                personToUnit = data.PersonsToUnits.Add(personToUnit);
            }
            personToUnit.PositionNameShort = positionNameShort;

        }



        public void UpdateInfoFromProfilePage(string personName)
        {
            lock (this)
            {
                Log.Trace("updating profile for " + personName + " start");

                var url = Person.GetPersonProfilePageUrl(personName);
                var response = session.GetUrl(url);
                var html = response.HtmlDocument;

                using (data = db.NewContext)
                {
                    var person = data.Persons.FirstOrDefault(p => p.Name == personName);
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

                }

                Log.Trace("updating profile for " + personName + " end");
            }

        }


        DateTime ParseUsTime([NotNull] string str)
        {
            //return DateTime.ParseExact(str, "M/d/yyyy hh:mm:ss zzz", CultureInfo.InvariantCulture);
            return DateTime.Parse(str);
        }


        public void ParseEventData(int eventTawId)
        {
            lock (this)
            {
                var url = Event.GetEventPage(eventTawId);
                var response = session.GetUrl(url);
                using (data = db.NewContext)
                {
                    ParseEventData_1(response);
                }
            }
        }

        void ParseEventData_1(MyHttpWebResponse response)
        {
            var uriPath = response.ResponseUri.AbsolutePath;
            if (uriPath.Contains("event") == false)
            {
                Log.Error("the event you are trying to parse has invalid uri");
            }

            var eventTawIdStr = uriPath.Split('/', '\\').Last().RemoveFromEnd(".aspx".Length);
            var eventTawId = int.Parse(eventTawIdStr);

            var evt = data.Events.FirstOrDefault(e => e.TawId == eventTawId);
            if (evt == null)
            {
                evt = new Event();
                evt.TawId = eventTawId;
                evt = data.Events.Add(evt);
            }
            ParseEventData_2(evt, response.ResponseText);
        }

        void ParseEventData_2(Event evt, string htmlText)
        {
            // this page is so badly coded the HTML is invalid, chrome shows it correctly though, kudos to it
            // but HtmlAgilityPack just fails on it

            htmlText = htmlText?.TakeStringAfter("ctl00_ctl00_bcr_bcr_UpdatePanel\">");



            var eventInfoText = htmlText?.TakeStringBetween("<table cellpadding=\"20\" cellspacing=\"5\">", "</table>");
            if (eventInfoText == null) return; // "This is a Base Event and should never be seen. Please report this Issue." http://taw.net/event/65132.aspx

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
            evt.name = eventInfo["Name"];
            evt.description = eventInfo["Description"];
            evt.type = eventInfo["Type"];
            evt.mandatory = eventInfo["Mandatory"] == "Yes";
            evt.cancelled = eventInfo["Cancelled"] == "Yes";


            var when = eventInfo["When"];

            var strFrom = when.TakeStringBetween("from:", "to:", StringComparison.InvariantCultureIgnoreCase).Trim();
            if (strFrom != null) evt.from = ParseUsTime(strFrom);

            var strTo = when.TakeStringAfter("to:", StringComparison.InvariantCultureIgnoreCase).Trim();
            if (strTo != null) evt.to = ParseUsTime(strTo);



            var attendeesText = htmlText.TakeStringBetween("<table width=100%>", "</table>");
            var attendessDoc = new HtmlDocument();
            attendessDoc.LoadHtml(attendeesText);
            var attendeesTable = new HtmlTable(attendessDoc.DocumentNode);

            foreach (var attended in evt.Attended)
            {
                attended.Person.Attended.Remove(attended);
            }
            evt.Attended.Clear();

            foreach (var row in attendeesTable)
            {
                var name = row[0]?.InnerText?.Trim();
                var nameHref = row[0].SelectSingleNode("a").GetAttributeValue("href", "");
                if (nameHref.StartsWith("/member"))
                {
                    var attended = new PersonToEvent();
                    attended.Event = evt;

                    attended.Person = GetPersonFromName(name);

                    var attendanceStr = row[1]?.InnerText?.Trim();
                    AttendanceType attendanceType = AttendanceType.Unknown;
                    if (attendanceStr != null && Enum.TryParse(attendanceStr.ToLowerInvariant(), true, out attendanceType)) attended.AttendanceType = attendanceType;

                    var timestampStr = row[2]?.InnerText?.Trim();
                    DateTime timestamp;
                    //if (DateTime.TryParseExact(timestampStr, "MM-dd-yyyy hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp)) attended.timeStamp = timestamp;
                    if (DateTime.TryParse(timestampStr, out timestamp)) attended.TimeStamp = timestamp;

                    attended.Person.Attended.Add(attended);
                    evt.Attended.Add(attended);
                }
                else if (nameHref.StartsWith("/unit"))
                {
                    var unitTawIdStr = nameHref.Split('/', '\\').Last().RemoveFromEnd(".aspx".Length);
                    var unitTawId = int.Parse(unitTawIdStr);
                    evt.unit = GetUnit(unitTawId, name);
                }
                else
                {
                    throw new Exception("something is wrong, found unexpected data");
                }

            }


        }

    }
}
