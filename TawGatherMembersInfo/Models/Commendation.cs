using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TawGatherMembersInfo.Models
{
	public enum CommendationType
	{
		Unknown = 0,
		Medal = 1,
		Badge = 2,
		Tab = 3,
	}

	public class Commendation
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public virtual long Id { get; set; }

		public virtual string Name { get; set; }
		public virtual CommendationType Type { get; set; }
		public virtual string Image { get; set; }
	}
}