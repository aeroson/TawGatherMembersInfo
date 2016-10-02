using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TawGatherMembersInfo.Models
{
	public class PersonCommendationComment
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonCommendationCommentId { get; set; }

		public virtual DateTime Date { get; set; }

		[MaxLength]
		public virtual string Comment { get; set; }

		public virtual Person Person { get; set; }
		public virtual PersonCommendation PersonCommendation { get; set; }
	}
}