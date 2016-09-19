using Neitri;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TawGatherMembersInfo
{
	public class Program
	{
		public static IDependencyManager dependency = new Neitri.DependencyInjection.DependencyManager();

		HttpServerHandler httpServer;

		[Dependency(Register = true)]
		Config config;

		[Dependency(Register = true)]
		FileSystem fileSystem;

		[Dependency(Register = true)]
		DbContextProvider db;

		[Dependency(Register = true)]
		RoasterManager roaster;

		static void Main(string[] args)
		{
			new Program(args);
		}

		Program(string[] args)
		{
			dependency.BuildUp(this);
			config.LoadFile(fileSystem.GetFile("data", "config.xml"));

			{
				var log = new Neitri.Logging.LogAgregator();
				dependency.Register(log);

				log.AddLogger(new Neitri.Logging.LogConsole());

				var logFile = fileSystem.BaseDirectory.GetDirectory("data", "logs").CreateIfNotExists().GetFile(DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + ".txt");
				var sw = new StreamWriter(logFile);
				sw.AutoFlush = true;
				log.AddLogger(new Neitri.Logging.LogFile(sw));

				TawGatherMembersInfo.Log.log = log;
				dependency.Register(log);
			}

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

		void Start(string[] args)
		{
			// DB TEST
			var u = db.NewContext.RootUnit;

			//TestPrintAttendanceReport();

			httpServer = dependency.Create<HttpServerHandler>();

			roaster.Run();
			httpServer.Run();
		}

		void TestPrintAttendanceReport()
		{
			using (var ctx = db.NewContext)
			{
				var armaUnit = ctx.Units.First(u => u.TawId == 2776);
				var people = armaUnit.GetAllPeople();

				Console.WriteLine("UnitName	UserName	Rank	Trainings	Attended	Excused	AWOL	Unknown	Mandatory AVG	Total AVG	Days In Rank");
				foreach (var person in people)
				{
					var allEvents = person.Attended.Where(a => a.Event.Cancelled == false);
					var trainings = allEvents.Count();
					var attended = allEvents.Count(a => a.AttendanceType == Models.AttendanceType.Attended);
					var excused = allEvents.Count(a => a.AttendanceType == Models.AttendanceType.Excused);
					var awol = allEvents.Count(a => a.AttendanceType == Models.AttendanceType.Missed);

					var mandatoryEvents = allEvents.Where(a => a.Event.Mandatory);
					var mandatoryEventsCount = mandatoryEvents.Count();
					var mandatoryEventsAttended = mandatoryEvents.Count(a => a.AttendanceType == Models.AttendanceType.Attended);
					var mandatoryAvg = 0;
					if (mandatoryEventsAttended > 0) mandatoryAvg = (int)Math.Ceiling(100 * mandatoryEventsAttended / (float)mandatoryEventsCount);

					Console.WriteLine(
						person.TeamSpeakUnit.Name + "\t" +
						person.Name + "\t" +
						person.RankNameLong + "\t" +
						trainings + "\t" +
						attended + "\t" +
						excused + "\t" +
						awol + "\t" +
						mandatoryAvg + "%\t" +
						"0" + "\t" +
						"0" + "\t"
					);
				}
			}
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
		delegate bool ConsoleEventDelegate(int eventType);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
	}
}