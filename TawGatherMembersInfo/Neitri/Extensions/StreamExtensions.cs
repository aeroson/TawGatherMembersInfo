using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neitri
{
	public static class StreamExtensions
	{
		public static string StreamReadTextToEnd(this Stream s)
		{
			using (var sr = new StreamReader(s))
			{
				return sr.ReadToEnd();
			}
		}
	}
}
