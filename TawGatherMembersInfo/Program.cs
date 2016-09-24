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

		/*
				db.DropProcedureIfExists("AttendanceReport");
				var proc = db.CreateProcedure("AttendanceReport")
					.AddInParam<long>("rootUnitId")
					.AddInParam<int>("daysBackTo");
				proc
					.DeclareCursor("selected_people", db.Units.Where(u=>u.TawId = -1 or proc["rootUnitId"] ))
					.DeclareTemporaryTable<Model>("attendanceReportResult")
					.TruncateTemporaryTable("attendanceReportResult");

					.ForEachCursor("selected_people")
						.InsertInto("attendanceReportResult")
							.Values(

							);
				proc.SelectAllFromTemporaryTable("attendanceReportResult")

				delimiter //
				drop procedure if exists AttendanceReport//
		create procedure AttendanceReport(in rootUnitId bigint(20), in daysBackTo int(10)) -- , in daysBackFrom int(10)
		begin

			declare selected_PersonId bigint(20);
				declare cursor_end tinyint(1);

				declare totalMandatories bigint(20);
				declare totalAnyEvent bigint(20);

				declare startDate datetime;
			declare endDate datetime;

			declare selected_people cursor for
				select p.PersonId from People p
				join PeopleToUnits p2u on p2u.PersonId = p.PersonId and p2u.UnitId in
				(
					select* from
						(select battalion.UnitId from Units battalion where battalion.TawId = rootUnitId) a
					union all
						(select platoon.UnitId from Units battalion
						join Units platoon on battalion.UnitId = platoon.ParentUnit_UnitId and battalion.TawId = rootUnitId)
					union all
						(select squad.UnitId from Units battalion
						join Units platoon on battalion.UnitId = platoon.ParentUnit_UnitId and battalion.TawId = rootUnitId
						join Units squad on platoon.UnitId = squad.ParentUnit_UnitId)
					union all
						(select fireteam.UnitId from Units battalion
						join Units platoon on battalion.UnitId = platoon.ParentUnit_UnitId and battalion.TawId = rootUnitId
						join Units squad on platoon.UnitId = squad.ParentUnit_UnitId
						join Units fireteam on squad.UnitId = fireteam.ParentUnit_UnitId)
				)
				group by p.PersonId
				order by name;

				declare continue handler for not found set cursor_end = true;

				create temporary table if not exists attendanceReportResult(
					UnitName varchar(100),
				UserName varchar(500),
				RankNameShort varchar(10),
				Trainings bigint(20),
				Attended bigint(20),
				Excused bigint(20),
				AWOL bigint(20),
				MandatoryAVG float,
				TotalAVG float,
				DaysInRank bigint(20)
			);

			truncate table attendanceReportResult;
			select(date_sub(now(), interval daysBackTo day)) into startDate;

				open selected_people;
				read_loop: LOOP

					fetch selected_people into selected_PersonId;

				if cursor_end then
					leave read_loop;
				end if;

				select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > startDate
				and e.Mandatory
				into totalMandatories;

				select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > startDate
				into totalAnyEvent;

				insert into attendanceReportResult values(

					(select u.Name from Units u where u.TawId = rootUnitId),
					(select p.Name from People p where p.PersonId = selected_PersonId),
					(select p.RankNameShort from People p where p.PersonId = selected_PersonId),

					(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > startDate
					),

					(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > startDate
					and pe.AttendanceType = 1),

					(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > startDate
					and pe.AttendanceType = 2),

					(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > startDate
					and pe.AttendanceType = 3),

					IF(
						totalMandatories > 0,
						(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > startDate
						and pe.AttendanceType = 1 and e.Mandatory) / totalMandatories,
						0
					),

					IF(
						totalAnyEvent > 0,
						(select count(*) from PeopleToEvents pe join People p on p.PersonId = selected_PersonId and p.PersonId = pe.PersonId join Events e on e.EventId = pe.EventId and e.From > startDate
						and pe.AttendanceType = 1) / totalAnyEvent,
						0
					),

					0
				);

			end loop;

				close selected_people;

				select* from attendanceReportResult order by UserName;

				end//
				delimiter;
		*/

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