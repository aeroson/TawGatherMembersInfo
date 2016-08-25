using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neitri;

namespace TawGatherMembersInfo
{
	public static class Log
	{
		public static ILogging log;
		public static void Error<T>(T value)
		{
			log.Error(value);
		}

		public static void Fatal<T>(T value)
		{
			log.Fatal(value);
		}

		public static void Info<T>(T value)
		{
			log.Info(value);
		}

		public static void Trace<T>(T value)
		{
			log.Trace(value);
		}

		public static void Warn<T>(T value)
		{
			log.Warn(value);
		}
	}
}
