using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TawGatherMembersInfo.Models
{
	public class PersonStatus
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonStatusId { get; set; }

		public enum StatusType
		{
			Other,
			ReturnedToActiveDuty,
			PutOnLeave,
			Reinstated,
			Discharged,
			DischargedHonorably,
			DischargedDishonorably,
		}

		[MaxLength]
		public virtual string Other { get; set; }

		public virtual StatusType Type { get; set; } = StatusType.Other;
		public virtual DateTime Date { get; set; }
		public virtual Person Person { get; set; }
		public virtual Person ByWho { get; set; }
	}
}