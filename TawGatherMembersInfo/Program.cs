using Neitri;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class Program
	{
		public static IDependencyManager dependency = new Neitri.DependencyInjection.DependencyManager();

		Config config;
		FileSystem fileSystem;
		public static ILogEnd Log { get; private set; }

		[Dependency(Register = true)]
		DbContextProvider db;

		[Dependency(Register = true)]
		RoasterManager roaster;

		[Dependency(Register = true)]
		HttpServerHandler httpServer;

		static void Main(string[] args)
		{
			new Program(args);
		}

		Program(string[] args)
		{
			fileSystem = new FileSystem();
			fileSystem.FullPath = fileSystem.GetDirectory("data").CreateIfNotExists().FullPath;

			config = new Config();
			config.LoadFile(fileSystem.GetFile("config.xml"));

			{
				var a = new Neitri.Logging.LogAgregator();
				Log = a;

				a.AddLogger(new Neitri.Logging.LogConsole());

				var logFile = fileSystem.GetDirectory("logs").CreateIfNotExists().GetFile(DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + ".txt");
				var sw = new StreamWriter(logFile);
				sw.AutoFlush = true;
				a.AddLogger(new Neitri.Logging.LogFile(sw));
			}

			dependency.Register(fileSystem, config, Log);

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
			using (var data = db.NewContext)
			{
				if (data.People.Count() == 0) // seed people from backed up order
				{
					Log.Info("no people found, seeding people from backed up people order file, start");
					var people = File.ReadAllLines(fileSystem.GetFile("backup.personsOrder.txt").ExceptionIfNotExists());
					foreach (var personName in people)
					{
						var person = new Person();
						person.Name = personName;
						data.People.Add(person);
						data.SaveChanges();
					}
					Log.Info("no people found, seeding people from backed up people order file, end");
				}
			}

			using (var data = db.NewContext)
			{
				var s = data.People.FirstOrDefault(p => p.Name.StartsWith("Sidthe"));
				var a = s.TeamSpeakName;
			}

			//TestPrintAttendanceReport();
			//AttendanceStatisticsPerWeekDay();

			//httpServer = dependency.Create<HttpServerHandler>();

			//UpdateSquadXml();

#if !DEBUG
			roaster.OnDataGatheringCycleCompleted += UpdateSquadXml;
			roaster.Run();
#endif

			httpServer?.Run();
		}

		void AttendanceStatisticsPerWeekDay()
		{
			using (var data = db.NewContext)
			{
				var events = data.Events.Where(e => e.Mandatory && !e.Cancelled);

				//Console.WriteLine("unit: " + units.Select(u => u.ToString()).Join(","));
				Console.WriteLine("not cancelled mandatory events count: " + events.Count());

				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Sunday);
				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Monday);
				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Tuesday);
				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Wednesday);
				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Thursday);
				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Friday);
				AttendanceStatisticsPerWeekDay_2(events, DayOfWeek.Saturday);
			}
		}

		void AttendanceStatisticsPerWeekDay_2(IEnumerable<Event> _events, DayOfWeek dayOfWeek)
		{
			Console.WriteLine("");

			var events = _events.Where(e => e.From.DayOfWeek == dayOfWeek).OrderBy(e => e.From);

			Console.WriteLine("dayOfWeek: " + dayOfWeek);
			var eventsCount = events.Count();
			Console.WriteLine("not cancelled mandatory events count: " + eventsCount);
			if (eventsCount == 0) return;

			Console.WriteLine("first event: " + events.First());
			Console.WriteLine("last event: " + events.Last());

			var _attended = events.SelectMany(e => e.Attended);
			var invited = _attended.Count();
			var awol = _attended.Where(a => a.AttendanceType == AttendanceType.Missed).Count();
			var excused = _attended.Where(a => a.AttendanceType == AttendanceType.Excused).Count();
			var attended = _attended.Where(a => a.AttendanceType == AttendanceType.Attended).Count();

			Console.WriteLine($"{nameof(invited)}: {invited}");
			Console.WriteLine($"{nameof(attended)}: {attended}");
			Console.WriteLine($"{nameof(awol)}: {awol}");
			Console.WriteLine($"{nameof(excused)}: {excused}");
			Console.WriteLine($"{nameof(attended)}/{nameof(invited)}: {attended / (float)invited}");
		}

		void TestPrintAttendanceReport()
		{
			using (var ctx = db.NewContext)
			{
				var armaUnit = ctx.Units.First(u => u.TawId == 2776);
				var people = armaUnit.GetAllActivePeople();

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
						person.TeamSpeakUnit.Unit.Name + "\t" +
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
			roaster?.Join();
			httpServer?.Join();
		}

		void Stop()
		{
			roaster?.Stop();
			httpServer?.Stop();
		}

		// its pain in the ass to detect close in C#
		//http://stackoverflow.com/questions/4646827/on-exit-for-a-console-application
		delegate bool ConsoleEventDelegate(int eventType);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
	}
}