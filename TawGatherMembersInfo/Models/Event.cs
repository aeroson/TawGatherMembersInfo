using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{
	// http://taw.net/member/aeroson/events/all.aspx
	public class Event : IEquatable<Event>
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long EventId { get; set; }

		[Index(IsUnique = true)]
		public virtual long TawId { get; set; }

		[StringLength(500)]
		public virtual string Name { get; set; }

		[Column(TypeName = "text")]
		public virtual string Description { get; set; }

		[StringLength(500)]
		public virtual string Type { get; set; }

		public virtual Unit Unit { get; set; }
		public virtual bool Mandatory { get; set; }
		public virtual bool Cancelled { get; set; }
		public virtual DateTime From { get; set; }
		public virtual DateTime To { get; set; }
		public virtual Person TakenBy { get; set; } // attendance taken by
		public virtual ICollection<PersonToEvent> Attended { get; set; }

		public static string GetEventPage(long eventTawId)
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
			return Name + " desc:" + Description;
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