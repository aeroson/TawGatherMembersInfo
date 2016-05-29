using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;

using HandlebarsDotNet;

namespace TawGatherMembersInfo
{
    public class InstancesContainer
    {
        public RoasterFactoryHandler roaster;
        public HttpServerHandler httpServer;
        public XMLConfig config;
    }

    class Program
    {
        static void Main(string[] args)
        {
            new Program(args);
        }

        InstancesContainer instaces = new InstancesContainer();


        void Start(string[] args)
        {




            var config = instaces.config = new XMLConfig();
            config.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "config.xml"));


            short port = short.Parse(config.Get("httpServerPort", "8000"));

            var roaster = instaces.roaster = new RoasterFactoryHandler(instaces);

            var httpServer = instaces.httpServer = new HttpServerHandler(instaces, port);

            roaster.Run();
            httpServer.Run();


            roaster.OnRoasterDataUpdated += UpdateArma3SquadXml;


        }

        // try to get picture from teamSpeakUnit and go up the parent chain if not found
        static string GetUnitImage(Unit rootUnit, Unit unit, string targetSquadXmlFolder)
        {
             if (unit == null) return null;

            var unitImage = unit.id + ".paa";
            var unitImageExists = File.Exists(Path.Combine(targetSquadXmlFolder, unitImage));
            if (unitImageExists) return unitImage;

            if (unitImageExists == false && unit.parentUnit != null)
            {
                unitImage = unit.parentUnit.id + "-child.paa";
                unitImageExists = File.Exists(Path.Combine(targetSquadXmlFolder, unitImage));
                if (unitImageExists) return unitImage;
            }

            if (unit == rootUnit) return null;
            return GetUnitImage(rootUnit, unit.parentUnit, targetSquadXmlFolder);
        }

        void UpdateArma3SquadXml()
        {           
            var rootUnit = instaces.roaster.CurrentData.idToUnit.Get(2776, null); // 2776 == Arma 3 Division
            if (rootUnit == null) return;

            var targetSquadXmlFolder = instaces.config.Get("targetSquadXmlFolder", "squadxml");
            string source = File.ReadAllText(Path.Combine(targetSquadXmlFolder, "{{name}}.xml.handlebars"));
            var template = Handlebars.Compile(source);

            Console.WriteLine("generating squad xml into: '" + targetSquadXmlFolder + "'");

            foreach (var person in rootUnit.GetAllPersons())
            {

                var picture = GetUnitImage(rootUnit, person.MostImportantIngameUnit, targetSquadXmlFolder) ?? "taw_paa.paa";            
  
                var result = template(
                    new
                    {
                        nick = "TAW.net",
                        name = person.MostImportantIngameUnit.name,
                        email = person.MostImportantIngameUnit.HighestRankingPerson.name.ToLower() + "@taw.net",
                        web = "http://www.taw.net",
                        picture = picture,
                        title = "TAW - " + person.MostImportantIngameUnit.name,
                        members = new[]
                        {
                            new
                            {
                                id = person.steamId,
                                nick = person.TeamSpeakName,
                                name = person.name,
                                email = person.name.ToLower() + "@taw.net",
                                icq = person.TeamSpeakUnit.name + " - " + person.TeamSpeakUnitPositionNameLong,
                                remark = "Join us at www.TAW.net",
                            }
                        }
                    }
                );

                File.WriteAllText(Path.Combine(targetSquadXmlFolder, person.name + ".xml"), result);

            }

            Console.WriteLine("done generating squad xml");

        }

        void Stop()
        {
            instaces.roaster.Stop();
            instaces.httpServer.Stop();
        }




        public Program(string[] args)
        {
            try
            {
                Console.WriteLine("Starting ...");
                Start(args);
                Console.WriteLine("Started");

                AppDomain.CurrentDomain.ProcessExit += (sender, a) =>
                {
                    Stop();
                };
                /*
                var handler = new ConsoleEventDelegate((int eventType) =>
                {
                    if (eventType == 2)
                    {
                        Stop();
                    }
                    return true;
                });
                SetConsoleCtrlHandler(handler, true);
                */

                while (Thread.CurrentThread.ThreadState == ThreadState.Running) Thread.Sleep(100);

                Console.WriteLine("Stopping, this may take a while ...");
                Stop();
                Console.WriteLine("Stopped");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }

        }


        // its pain in the ass to detect close in C#
        //http://stackoverflow.com/questions/4646827/on-exit-for-a-console-application
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);




    }

}