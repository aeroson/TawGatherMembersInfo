using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace TawGatherMembersInfo.Models
{
	public class PersonUnit
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonUnitId { get; set; }

		/*
		[ForeignKey(nameof(Person))]
		public long Person_PersonId { get; set; }

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

		public virtual Person Person { get; set; }
		public virtual Unit Unit { get; set; }
	}
}