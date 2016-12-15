using Neitri;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TawGatherMembersInfo
{
	public static class Extensions
	{
		public static void WaitAll(this List<Task> tasks, ILogEnd log = null)
		{
			try
			{
				Task.WaitAll(tasks.ToArray());
			}
			catch (Exception e)
			{
				if (log == null) throw;
				else log.FatalException(e);
			}
		}

		public static async Task WaitAllAsync(this List<Task> tasks, ILogEnd log = null)
		{
			try
			{
				await Task.WhenAll(tasks.ToArray());
			}
			catch (Exception e)
			{
				if (log == null) throw;
				else log.FatalException(e);
			}
		}
	}
}
