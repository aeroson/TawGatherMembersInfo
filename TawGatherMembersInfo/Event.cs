using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TawGatherMembersInfo
{
	// http://taw.net/member/aeroson/events/all.aspx
	public class Event
	{
		public int tawId;
		public string name;
		public string description;
		public string type;
		public string unityName;
		public Unit unit
		{
			get
			{
				return null;
			}
		}
		public bool mandatory;
		public bool cancelled;
		public DateTime from;
		public DateTime to;

		public class Invite
		{
			public string personName;
			public Person person
			{
				get
				{
					return null;
				}
			}
			public string attended;

		}
		public List<Invite> invitees = new List<Invite>();

	}
}
