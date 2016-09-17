using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{
	[Serializable]
	public class PersonToUnit
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		public virtual Person Person { get; set; }
		public virtual Unit Unit { get; set; }
		public virtual string PositionNameShort { get; set; }
	}
}