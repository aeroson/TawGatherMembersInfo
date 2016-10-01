using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{
	public class PersonUnit
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonUnitId { get; set; }

		/*
		[Index]
		public long Person_PersonId { get; set; }

		[Index]
		public long Unit_UnitId { get; set; }
		*/

		[StringLength(500)]
		public virtual string PositionNameShort { get; set; }

		public virtual Person JoinedBy { get; set; }

		[Index]
		public virtual DateTime Joined { get; set; }

		public virtual Person RemovedBy { get; set; }

		[Index]
		public virtual DateTime Removed { get; set; }

		public virtual Person ForPerson { get; set; }
		public virtual Unit ForUnit { get; set; }
	}
}