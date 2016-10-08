using System;
using System.Diagnostics;

namespace Neitri
{
	public class Profiler : IDisposable
	{
		string name;
		Stopwatch time;
		ILogEnd log;

		public Profiler(string name, ILogEnd log)
		{
			this.name = name;
			this.time = new Stopwatch();
			this.time.Start();
			this.log = log;
		}

		public void Dispose()
		{
			this.time.Stop();
			log.Trace("'" + name + "' took " + time.ElapsedMilliseconds + " ms");
		}
	}
}