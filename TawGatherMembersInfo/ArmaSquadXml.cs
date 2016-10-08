using HandlebarsDotNet;
using Neitri;
using System;
using System.IO;
using System.Linq;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class ArmaSquadXml
	{
		[Dependency]
		DbContextProvider db;

		[Dependency]
		Config config;

		[Dependency]
		FileSystem fileSystem;

		MyDbContext data;

		static ILogEnd Log => Program.Log;

		public void UpdateArma3SquadXml()
		{
			using (data = db.NewContext)
			{
				Private_UpdateArma3SquadXml();
			}
		}

		void Private_UpdateArma3SquadXml()
		{
			// var rootUnit = instances.roaster.CurrentData.idToUnit.GetValue(2776, null); // 2776 == Arma 3 Division
			// if (rootUnit == null) return;

			var targetSquadXmlFolder = fileSystem.GetDirectory("squadxml");
			string source = File.ReadAllText(targetSquadXmlFolder.GetFile("template.handlebars").ExceptionIfNotExists());
			var template = Handlebars.Compile(source);

			Log.Info("generating squad xmls into: '" + targetSquadXmlFolder + "'");

			targetSquadXmlFolder.FindFiles("*.xml").ForEach(f => f.Delete());

			var utcNow = DateTime.UtcNow;
			var people = data.People.Where(p => p.Units.Count(u => u.Removed > utcNow) > 0).ToArray();

			foreach (var person in people)
			{
				// TODO: skip discharged persons
				var armaProfileName = person.Biography.GetData("profile name", "arma profile name");
				if (armaProfileName.IsNullOrEmpty())
				{
					//if (person.IsTeamSpeakNameGuaranteedToBeCorrect == false) continue;
					armaProfileName = person.TeamSpeakName;
					if (armaProfileName.Contains("[]")) continue; // taw teamspeak name position suffix is empty, failed to figure it out
				}

				var image = GetUnitImage(person, targetSquadXmlFolder);

				Log.Trace("generating squad xml for: " + person.Name + " image:" + image.Name + " armaProfileName:" + armaProfileName);

				var showUnit = person.MostImportantIngameUnit;
				if (showUnit.Type.ToLower() == "fire team" && showUnit.ParentUnit != null && showUnit.ParentUnit.People.Count > 0) showUnit = showUnit.ParentUnit;

				var rendered = template(
					new
					{
						nick = "TAW.net",
						name = showUnit.Name,
						email = showUnit.HighestRankingPerson?.Name?.ToLower() + "@taw.net",
						web = "http://www.taw.net",
						picture = image.Name,
						title = "TAW - " + showUnit.Name,
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
								icq = person.TeamSpeakUnit.Name + " - " + person.TeamSpeakUnitPositionNameLong,
								remark = "Join us at www.TAW.net",
							}
						}
					}
				);

				File.WriteAllText(targetSquadXmlFolder.GetFile(person.Name + ".xml"), rendered);
			}

			Log.Info("done generating squad xmls");
		}

		FilePath GetUnitImage(Person person, DirectoryPath targetSquadXmlFolder)
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
					file = targetSquadXmlFolder.GetFile(unit.TawId.ToString() + ".paa");
					if (file.Exists)
					{
						var t = unit.Type.ToLower();
						if ((t == "battalion" || t == "platoon" || t == "division") && unit.PersonToPositionNameShort.GetValue(person).IsNullOrEmpty())
						{
							// if our unit for which we have image is either battalion or division, we also need to hold position in it,
							// to prevent recruits from getting command image, i.e.: http://image.prntscr.com/image/d10305807da8433a88a9dbe06d8147e9.png
						}
						else return file;
					}

					file = targetSquadXmlFolder.GetFile(unit.TawId.ToString() + "-child.paa");
					if (file.Exists) return file;

					unit = unit.ParentUnit; // walk up the tree;
				}
				while (unit != null);
			}

			file = targetSquadXmlFolder.GetFile("default.paa");
			return file;
		}
	}
}