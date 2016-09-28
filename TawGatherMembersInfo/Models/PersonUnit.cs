using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{
	public class PersonUnit
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		[Index]
		public long PersonId { get; set; }

		[Index]
		public long UnitId { get; set; }

		[StringLength(500)]
		public virtual string PositionNameShort { get; set; }

		public virtual Person JoinedBy { get; set; }
		public virtual DateTime Joined { get; set; }

		public virtual Person RemovedBy { get; set; }
		public virtual DateTime Removed { get; set; }

		public virtual Person Person { get; set; }
		public virtual Unit Unit { get; set; }
	}
}