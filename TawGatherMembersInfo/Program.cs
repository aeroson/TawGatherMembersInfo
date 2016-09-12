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

		RoasterManager roaster;
		HttpServerHandler httpServer;
		Config config;

		void Start(string[] args)
		{
			config = dependency.CreateAndRegister<Config>();
			config.LoadFile(fileSystem.GetFile("data", "config.xml"));

			roaster = dependency.CreateAndRegister<RoasterManager>();
			httpServer = dependency.CreateAndRegister<HttpServerHandler>();

			roaster.Run();
			httpServer.Run();

			roaster.OnRoasterDataUpdated += UpdateArma3SquadXml;
		}

		void Join()
		{
			roaster.Join();
			httpServer.Join();
		}

		static FilePath GetUnitImage(Unit rootUnit, Person person, DirectoryPath targetSquadXmlFolder)
		{
			// try logo defined in taw profile biography
			var image = person.Biography.GetData("squadxml logo", "arma squadxml logo");
			if (image.IsNullOrEmpty() == false && image.EndsWith(".paa") == false) image += ".paa";
			var file = targetSquadXmlFolder.GetFile(image);
			if (file.Exists) return file;

			// try logo from our unit
			{
				var unit = person.MostImportantIngameUnit;

				do
				{
					file = targetSquadXmlFolder.GetFile(unit.id.ToString() + ".paa");
					if (file.Exists)
					{
						var t = unit.type.ToLower();
						if ((t == "battalion" || t == "platoon" || t == "division") && unit.personToPositionNameShort.GetValue(person).IsNullOrEmpty())
						{
							// if our unit for which we have image is either battalion or division, we also need to hold position in it,
							// to prevent recruits from getting command image, i.e.: http://image.prntscr.com/image/d10305807da8433a88a9dbe06d8147e9.png
						}
						else return file;
					}

					file = targetSquadXmlFolder.GetFile(unit.id.ToString() + "-child.paa");
					if (file.Exists) return file;

					unit = unit.parentUnit; // walk up the tree;
				}
				while (unit != rootUnit && unit != null);

			}

			file = targetSquadXmlFolder.GetFile("default.paa");
			return file;
		}

		void UpdateArma3SquadXml()
		{
			// var rootUnit = instances.roaster.CurrentData.idToUnit.GetValue(2776, null); // 2776 == Arma 3 Division
			// if (rootUnit == null) return;

			var rootUnit = roaster.CurrentRoaster.rootUnit;

			var targetSquadXmlFolder = fileSystem.GetDirectory(config.GetValue("targetSquadXmlFolder", "squadxml"));
			string source = File.ReadAllText(targetSquadXmlFolder.GetFile("template.handlebars").ExceptionIfNotExists());
			var template = Handlebars.Compile(source);

			Log.Info("generating squad xmls into: '" + targetSquadXmlFolder + "'");

			foreach (var person in rootUnit.GetAllPersons())
			{
				// TODO: skip discharged perso is 
				var armaProfileName = person.Biography.GetData("profile name", "arma profile name");
				if (armaProfileName.IsNullOrEmpty())
				{
					//if (person.IsTeamSpeakNameGuaranteedToBeCorrect == false) continue;
					armaProfileName = person.TeamSpeakName;
				}
				
				var image = GetUnitImage(rootUnit, person, targetSquadXmlFolder);

				Log.Trace("generating squad xml for: " + person.Name + " image:" + image.Name + " armaProfileName:" + armaProfileName);

				var rendered = template(
					new
					{
						nick = "TAW.net",
						name = person.MostImportantIngameUnit.name,
						email = person.MostImportantIngameUnit.HighestRankingPerson.Name.ToLower() + "@taw.net",
						web = "http://www.taw.net",
						picture = image.Name,
						title = "TAW - " + person.MostImportantIngameUnit.name,
						fileGenerated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
						lastProfileDataUpdated = person.LastProfileDataUpdatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
						members = new[]
						{
							new
							{
								id = person.SteamId,
								nick = armaProfileName,
								name = person.Name,
								email = person.Name.ToLower() + "@taw.net",
								icq = person.TeamSpeakUnit.name + " - " + person.TeamSpeakUnitPositionNameLong,
								remark = "Join us at www.TAW.net",
							}
						}
					}
				);

				File.WriteAllText(targetSquadXmlFolder.GetFile(person.Name + ".xml"), rendered);

			}

			Log.Info("done generating squad xmls");

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