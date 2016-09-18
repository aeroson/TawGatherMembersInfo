using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{
	[Table("PeopleToUnits")]
	public class PersonToUnit
	{
		[Key, Column(Order = 0)]
		public long PersonId { get; set; }

		[Key, Column(Order = 1)]
		public long UnitId { get; set; }

		public virtual Person Person { get; set; }
		public virtual Unit Unit { get; set; }

		[StringLength(500)]
		public virtual string PositionNameShort { get; set; }
	}
}