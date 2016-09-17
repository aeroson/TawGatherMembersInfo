using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neitri.WebCrawling;
using Neitri;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{


    // http://taw.net/member/aeroson/events/all.aspx
    [Serializable]
    public class Event : IEquatable<Event>
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Index(IsUnique = true)]
        public virtual int TawId { get; set; }
        public virtual string name { get; set; }
        public virtual string description { get; set; }
        public virtual string type { get; set; }
        public virtual string unityName { get; set; }
        public virtual Unit unit { get; set; }
        public virtual bool mandatory { get; set; }
        public virtual bool cancelled { get; set; }
        public virtual DateTime from { get; set; }
        public virtual DateTime to { get; set; }
        public virtual Person takenBy { get; set; } // attendance taken by
        public virtual ICollection<PersonToEvent> Attended { get; set; }  = new HashSet<PersonToEvent>();


        public static string GetEventPage(int eventTawId)
        {
            return @"http://taw.net/event/" + eventTawId + ".aspx";
        }

        // under TS AL rank can see only his own events
        public static string GetAllMemberEventsPage(string memberName)
        {
            return @"http://taw.net/member/" + memberName + "/events/all.aspx";
        }


        public override string ToString()
        {
            return name + " desc:" + description;
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as Event);
        }
        public bool Equals(Event other)
        {
            if (other == null) return false;
            return TawId == other.TawId;
        }

        public override int GetHashCode()
        {
            return TawId.GetHashCode();
        }

        DateTime ParseUsTime([NotNull] string str)
        {
            //return DateTime.ParseExact(str, "M/d/yyyy hh:mm:ss zzz", CultureInfo.InvariantCulture);
            return DateTime.Parse(str);
        }
        public static void ParseEventData(RoasterData roaster, MyHttpWebResponse response)
        {
            var uriPath = response.ResponseUri.AbsolutePath;
            if (uriPath.Contains("event") == false)
            {
                throw new Exception("the event you are trying to parse has invalid uri");
            }

            var eventTawIdStr = uriPath.Split('/', '\\').Last().RemoveFromEnd(".aspx".Length);
            var eventTawId = int.Parse(eventTawIdStr);

            Event evt;
            if (roaster.idToEvent.TryGetValue(eventTawId, out evt))
            {
                evt.ParseEventData(roaster, response.ResponseText);
            }
            else
            {
                evt = new Event();
                evt.TawId = eventTawId;
                evt.ParseEventData(roaster, response.ResponseText);
                roaster.allEvents.Add(evt);
                roaster.idToEvent[evt.TawId] = evt;
            }
        }

        private void ParseEventData(RoasterData roaster, string htmlText)
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
            this.name = eventInfo["Name"];
            this.description = eventInfo["Description"];
            this.type = eventInfo["Type"];
            this.mandatory = eventInfo["Mandatory"] == "Yes";
            this.cancelled = eventInfo["Cancelled"] == "Yes";


            var when = eventInfo["When"];

            var strFrom = when.TakeStringBetween("from:", "to:", StringComparison.InvariantCultureIgnoreCase).Trim();
            if (strFrom != null) from = ParseUsTime(strFrom);

            var strTo = when.TakeStringAfter("to:", StringComparison.InvariantCultureIgnoreCase).Trim();
            if (strTo != null) to = ParseUsTime(strTo);



            var attendeesText = htmlText.TakeStringBetween("<table width=100%>", "</table>");
            var attendessDoc = new HtmlDocument();
            attendessDoc.LoadHtml(attendeesText);
            var attendeesTable = new HtmlTable(attendessDoc.DocumentNode);

            foreach (var attended in Attended)
            {
                attended.Person.Attended.Remove(attended);
            }
            Attended.Clear();

            foreach (var row in attendeesTable)
            {
                var name = row[0]?.InnerText?.Trim();
                var nameHref = row[0].SelectSingleNode("a").GetAttributeValue("href", "");
                if (nameHref.StartsWith("/member"))
                {
                    var attended = new PersonToEvent();
                    attended.Event = this;

                    attended.Person = roaster.GetOrCreateEmptyPersonFromName(name);

                    var attendanceStr = row[1]?.InnerText?.Trim();
                    AttendanceType attendanceType = AttendanceType.Unknown;
                    if (attendanceStr != null && Enum.TryParse(attendanceStr.ToLowerInvariant(), true, out attendanceType)) attended.AttendanceType = attendanceType;

                    var timestampStr = row[2]?.InnerText?.Trim();
                    DateTime timestamp;
                    //if (DateTime.TryParseExact(timestampStr, "MM-dd-yyyy hh:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp)) attended.timeStamp = timestamp;
                    if (DateTime.TryParse(timestampStr, out timestamp)) attended.TimeStamp = timestamp;

                    attended.Person.Attended.Add(attended);
                    Attended.Add(attended);
                }
                else if (nameHref.StartsWith("/unit"))
                {
                    var unitTawIdStr = nameHref.Split('/', '\\').Last().RemoveFromEnd(".aspx".Length);
                    var unitTawId = int.Parse(unitTawIdStr);
                    unit = roaster.GetOrCreateUnit(unitTawId, name);
                }
                else
                {
                    throw new Exception("something is wrong, found unexpected data");
                }

            }


        }
    }


}
