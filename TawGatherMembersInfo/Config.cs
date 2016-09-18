using Neitri;
using System.Collections.Generic;
using System.Linq;

namespace TawGatherMembersInfo
{
	public class Config : XMLConfig
	{
		public string MySqlConnectionString => this.Root.Descendants("mySqlConnectionString").First().Value;

		public IEnumerable<string> AuthTokens => this.Root.Descendants("authenticationTokens").First().Elements().Select(e => e.Value).ToList();
		public short HttpServerPort => short.Parse(this.GetValue("httpServerPort", "8000"));

		public IEnumerable<int> UnitIdsToGatherMemberProfileInfo => this.Root.Descendants("unitsToGatherMemberInfo").First().Elements().Select(e => int.Parse(e.Value));
	}
}