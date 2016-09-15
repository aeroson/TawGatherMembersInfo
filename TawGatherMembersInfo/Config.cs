using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neitri;

namespace TawGatherMembersInfo
{
	public class Config : XMLConfig
	{
		public IEnumerable<string> AuthTokens => this.Root.Descendants("authenticationTokens").First().Elements().Select(e => e.Value).ToList();
		public short HttpServerPort => short.Parse(this.GetValue("httpServerPort", "8000"));

		public IEnumerable<int> UnitIdsToGatherMemberProfileInfo => this.Root.Descendants("unitsToGatherMemberInfo").First().Elements().Select(e => int.Parse(e.Value));
	}
}
