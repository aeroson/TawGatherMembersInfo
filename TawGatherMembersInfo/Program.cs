using Neitri;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class Program
	{
		public static IDependencyManager dependency = new Neitri.DependencyInjection.DependencyManager();

		HttpServerHandler httpServer;

		Config config;
		FileSystem fileSystem;
		ILogging log;

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
			fileSystem = new FileSystem();
			fileSystem.BaseDirectory = fileSystem.BaseDirectory.GetDirectory("data").CreateIfNotExists();

			config = new Config();
			config.LoadFile(fileSystem.GetFile("config.xml"));

			{
				var a = new Neitri.Logging.LogAgregator();
				this.log = a;
				TawGatherMembersInfo.Log.log = a;

				a.AddLogger(new Neitri.Logging.LogConsole());

				var logFile = fileSystem.BaseDirectory.GetDirectory("logs").CreateIfNotExists().GetFile(DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + ".txt");
				var sw = new StreamWriter(logFile);
				sw.AutoFlush = true;
				a.AddLogger(new Neitri.Logging.LogFile(sw));
			}

			dependency.Register(fileSystem, config, log);

			dependency.BuildUp(this);

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
			using (var data = db.NewContext)
			{
				var u = data.RootUnit;
				var name = u.Name;
			}

			using (var data = db.NewContext)
			{
				if (data.People.Count() == 0) // seed people from backed up order
				{
					log.Info("no people found, seeding people from backed up people order file, start");
					var people = File.ReadAllLines(fileSystem.GetFile("backup.personsOrder.txt").ExceptionIfNotExists());
					foreach (var personName in people)
					{
						var person = new Person();
						person.Name = personName;
						data.People.Add(person);
						data.SaveChanges();
					}
					log.Info("no people found, seeding people from backed up people order file, end");
				}
			}

			//TestPrintAttendanceReport();
			//AttendanceStatisticsPerWeekDay();

			//httpServer = dependency.Create<HttpServerHandler>();

			//UpdateSquadXml();
			//roaster.OnDataGatheringCycleCompleted += UpdateSquadXml;
			roaster.Run();

			httpServer?.Run();
		}

		void AttendanceStatisticsPerWeekDay()
		{
			using (var data = db.NewContext)
			{
				var unit = data.Units.First(u => u.TawId == 1330);
				var events = unit.Events.Where(e => e.Mandatory && !e.Cancelled).ToArray();

				log.Info("unit: " + unit);
				log.Info("mandatory, not cancelled events count: " + events.Length);

				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Sunday);
				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Tuesday);
			}
		}

		void AttendanceStatisticsPerWeekDay_2(IEnumerable<Event> _events, DayOfWeek dayOfWeek)
		{
			var events = _events.Where(e => e.From.DayOfWeek == dayOfWeek).OrderByDescending(e => e.From).ToArray();
			var _attended = events.SelectMany(e => e.Attended).ToArray();

			log.Info("");
			log.Info("dayOfWeek: " + dayOfWeek);
			log.Info("mandatory, not cancelled events count: " + events.Length);
			log.Info("first event: " + events.First());
			log.Info("last event: " + events.Last());

			var invited = _attended.Length;
			var awol = _attended.Where(a => a.AttendanceType == AttendanceType.Missed).Count();
			var excused = _attended.Where(a => a.AttendanceType == AttendanceType.Excused).Count();
			var attended = _attended.Where(a => a.AttendanceType == AttendanceType.Attended).Count();

			log.Info($"{nameof(invited)}: {invited}");
			log.Info($"{nameof(attended)}: {attended}");
			log.Info($"{nameof(awol)}: {awol}");
			log.Info($"{nameof(excused)}: {excused}");
			log.Info($"{nameof(attended)}/{nameof(invited)}: {attended / (float)invited}");
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
					var allEvents = person.Events.Where(a => a.Event.Cancelled == false);
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
						person.Rank.NameLong + "\t" +
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