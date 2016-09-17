using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using Neitri;

using HandlebarsDotNet;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class InstancesContainer
	{
		public RoasterManager roaster;
		public HttpServerHandler httpServer;
		public XMLConfig config;
	}

	class Program
	{

		IDependencyManager dependency = new Neitri.DependencyInjection.DependencyManager();
		FileSystem fileSystem;

		static void Main(string[] args)
		{
			new Program(args);
		}

		public Program(string[] args)
		{
			fileSystem = dependency.CreateAndRegister<FileSystem>();

			{
				var log = new Neitri.Logging.LogAgregator();
				dependency.Register(log);

				log.AddLogger(new Neitri.Logging.LogConsole());

				var logFile = fileSystem.BaseDirectory.GetDirectory("data", "logs").CreateIfNotExists().GetFile(DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + ".txt");
				var sw = new StreamWriter(logFile);
				sw.AutoFlush = true;
				log.AddLogger(new Neitri.Logging.LogFile(sw));

				TawGatherMembersInfo.Log.log = log;
			}

			try
			{
				Log.Info("Starting ...");
				Start(args);
				AppDomain.CurrentDomain.ProcessExit += (sender, a) =>
				{
					Stop();
				};
				Log.Info("Started");

				Join();

				Log.Info("Stopping, this may take a while ...");
				Stop();
				Log.Info("Stopped");

			}
			catch (Exception e)
			{
				Log.Fatal(e);
				Console.ReadKey();
			}

		}

		HttpServerHandler httpServer;
		
		[Dependency(Register = true)]
		RoasterManager roaster;
		[Dependency(Register = true)]
		Config config;
		[Dependency(Register = true)]
		DbContextProvider db;

		void Start(string[] args)
		{
			dependency.BuildUp(this);

			config.LoadFile(fileSystem.GetFile("data", "config.xml"));
			httpServer = dependency.Create<HttpServerHandler>();

			roaster.Run();
			httpServer.Run();

			
		}

		void UpdateSquadXml()
		{
			var s = dependency.Create<ArmaSquadXml>();
			s.UpdateArma3SquadXml();
		}

		void Join()
		{
			roaster.Join();
			httpServer.Join();
		}

		
		void Stop()
		{
			roaster.Stop();
			httpServer.Stop();
		}






		// its pain in the ass to detect close in C#
		//http://stackoverflow.com/questions/4646827/on-exit-for-a-console-application
		private delegate bool ConsoleEventDelegate(int eventType);
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);




	}

}