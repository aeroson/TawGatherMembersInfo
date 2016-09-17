using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{
	public enum AttendanceType
	{
		Attended,
		Missed, // == AWOL
		Excused,
		Unknown
	}

	[Serializable]
	public class PersonToEvent : IEquatable<PersonToEvent>
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		public virtual Person Person { get; set; }
		public virtual Event Event { get; set; }
		public virtual AttendanceType AttendanceType { get; set; } = AttendanceType.Unknown;
		public virtual DateTime TimeStamp { get; set; }

		public override string ToString()
		{
			return Person.Name + "<->" + Event;
		}

		public bool Equals(PersonToEvent other)
		{
			if (other == null) return false;
			return Person.Id == other.Person.Id && Event.TawId == other.Event.TawId;
		}

		public override int GetHashCode()
		{
			return Person.GetHashCode() ^ Event.GetHashCode();
		}
	}
}