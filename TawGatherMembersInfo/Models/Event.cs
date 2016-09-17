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

 
    }


}
