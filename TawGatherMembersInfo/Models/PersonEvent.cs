using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{
	public enum AttendanceType
	{
		Unknown = 0,
		Attended = 1,
		Excused = 2,
		Missed = 3, // == AWOL
	}

	public class PersonEvent : IEquatable<PersonEvent>
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonEventId { get; set; }

		/*
		[Key, Column(Order = 0)]
		public long Person_PersonId { get; set; }

		[Key, Column(Order = 1)]
		public long Event_EventId { get; set; }
		*/

		[Index]
		public virtual AttendanceType AttendanceType { get; set; } = AttendanceType.Unknown;

		public virtual DateTime TimeStamp { get; set; }
		public virtual Event Event { get; set; }
		public virtual Person Person { get; set; }

		public override string ToString()
		{
			return Person.Name + "<->" + Event;
		}

		public bool Equals(PersonEvent other)
		{
			if (other == null) return false;
			return Person.PersonId == other.Person.PersonId && Event.TawId == other.Event.TawId;
		}

		public override int GetHashCode()
		{
			return Person.GetHashCode() ^ Event.GetHashCode();
		}
	}
}