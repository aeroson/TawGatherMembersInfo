using Neitri;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TawGatherMembersInfo
{
	public class Config : XMLConfig
	{
		public string MySqlConnectionString => GetOne("Server=[ip];Database=taw-db;Uid=root;Pwd=[password]");

		public IEnumerable<string> AuthenticationTokens => GetMany(new string[] { "example_KMzZA8Jm5E2AhF9MN5NY9t6eqYr5MWSp" });

		public short HttpServerPort => GetOne<short>(8000);

		public IEnumerable<int> UnitsToGatherMemberInfo => GetMany(new int[] { 2776 });

		/// <summary>
		/// Minimum is 2
		/// </summary>
		public int MaxConcurrentDatabaseConnections => Math.Max(GetOne(10), 2);

		public int MaxConcurrentWebSessions => GetOne(10);
		public int WebCrawlerLoopPauseSeconds => GetOne(10 * 60);

		public int MaxWebRequestsPerMinutePerSession => GetOne(60);
	}
}