using HandlebarsDotNet;
using Neitri;
using System;
using System.IO;
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

			var rootUnit = data.RootUnit;

			var targetSquadXmlFolder = fileSystem.GetDirectory(config.GetValue("targetSquadXmlFolder", "squadxml"));
			string source = File.ReadAllText(targetSquadXmlFolder.GetFile("template.handlebars").ExceptionIfNotExists());
			var template = Handlebars.Compile(source);

			Log.Info("generating squad xmls into: '" + targetSquadXmlFolder + "'");

			foreach (var person in rootUnit.GetAllPeople())
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
						name = person.MostImportantIngameUnit.Name,
						email = person.MostImportantIngameUnit.HighestRankingPerson.Name.ToLower() + "@taw.net",
						web = "http://www.taw.net",
						picture = image.Name,
						title = "TAW - " + person.MostImportantIngameUnit.Name,
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

		FilePath GetUnitImage(Unit rootUnit, Person person, DirectoryPath targetSquadXmlFolder)
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
					file = targetSquadXmlFolder.GetFile(unit.UnitId.ToString() + ".paa");
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

					file = targetSquadXmlFolder.GetFile(unit.UnitId.ToString() + "-child.paa");
					if (file.Exists) return file;

					unit = unit.ParentUnit; // walk up the tree;
				}
				while (unit != rootUnit && unit != null);
			}

			file = targetSquadXmlFolder.GetFile("default.paa");
			return file;
		}
	}
}