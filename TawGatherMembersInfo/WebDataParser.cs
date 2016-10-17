using HtmlAgilityPack;
using Neitri;
using Neitri.WebCrawling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public class WebDataParser
	{
		[Dependency]
		DbContextProvider db;

		[Dependency]
		SessionMannager sessionManager;

		public async Task UpdateUnitContents(ILogEnd log, SessionMannager sessionManager, int tawUnitId)
		{
			await new UpdateUnitContentsHandler(db).Run(log, sessionManager, tawUnitId);
		}

		class UpdateUnitContentsHandler
		{
			DbContextProvider db;
			Dictionary<string, List<UnitRoasterPersonLine>> personNameToPersonLines = new Dictionary<string, List<UnitRoasterPersonLine>>();

			public UpdateUnitContentsHandler(DbContextProvider db)
			{
				this.db = db;
			}

			public async Task Run(ILogEnd log, SessionMannager sessionManager, int tawUnitId)
			{
				var newVersion = new Random().Next(); // all person to unit relations without this unit version will be deleted

				log = log.ProfileStart($"roaster unit {tawUnitId}");

				var url = Unit.GetUnitRoasterPage(tawUnitId);

				var response = await sessionManager.GetUrl(url, log.ScopeStart("getting html"));

				var html = response.HtmlDocument;

				var roasterDiv = html.GetElementbyId("ctl00_bcr_UpdatePanel1").SelectSingleNode("./div/ul");

				using (log.ScopeStart("parsing html"))
					await ParseUnitContents(log, roasterDiv, null);

				using (var log2 = log.ScopeStart("parsing people"))
				{
					var tasks = new List<Task>(personNameToPersonLines.Count);

					foreach (var kvp in personNameToPersonLines)
					{
						var personName = kvp.Key;
						var personLines = kvp.Value;
						var task = Task.Run(async () =>
						{
							using (var data = db.NewContext)
							{
								log2.Trace("parsing & saving roaster person " + personName);
								foreach (var personLine in personLines)
								{
									await personLine.FinishParsing(log2, data);
								}
								var personUnitIds = personLines.Select(p => p.PersonToUnitId).ToArray();
								var utcNow = DateTime.UtcNow;
								// if some person to unit is still valid, and not one of those we just updated, mark it as not valid anymore
								data
										.People
										.First(p => p.Name == personName)
										.Units
										.Where(u => u.Removed > utcNow) // still valid, not removed
										.Where(u => !personUnitIds.Contains(u.PersonUnitId)) // except those we found & updated
										.ForEach(u => u.Removed = utcNow); // remove it

								try
								{
									await data.SaveChangesAsync();
								}
								catch (Exception e)
								{
									log2.Fatal(e);
								}
								log2.Trace("done parsing & saving roaster person:" + personName);
							}
						});
						tasks.Add(task);
					};
					await Task.WhenAll(tasks.ToArray());
				}
			}

			class UnitRoasterPersonLine
			{
				public string PersonName => name;

				public long PersonToUnitId { get; private set; }

				string name = "unnamed";
				string rank = "";
				string positionNameLong = "";
				string positionNameShort = "";
				bool onLeave = false;
				long unitId;

				public UnitRoasterPersonLine(ILogEnd log, string text, long unitId)
				{
					this.unitId = unitId;
					var dashIndex = text.LastIndexOf("-");

					if (dashIndex != -1)
					{
						var part1 = text.Substring(0, dashIndex - 1).Trim();
						var part2 = text.Substring(dashIndex + 1).Trim();

						if (part2.ToLower().Contains("on leave"))
						{
							onLeave = true;
							var parts = part1.Split(',');
							name = parts[0].Trim();
							rank = parts[1].Trim();
						}
						else
						{
							positionNameLong = part1;
							var parts = part2.Split(',');
							name = parts[0].Trim();
							rank = parts[1].Trim();
						}
					}
					else
					{
						var parts = text.Split(',');
						name = parts[0].Trim();
						rank = parts[1].Trim();
					}

					if (positionNameLong != "")
					{
						positionNameShort = Person.positionNameShortToPositionNameLong.Reverse.GetValue(positionNameLong, null);
						if (positionNameShort == null) log.Warn("cannot find positionNameShortToPositionNameLong.Reverse[" + positionNameLong + "]");
					}
				}

				async public Task FinishParsing(ILogEnd log, MyDbContext data)
				{
					/*
						text use cases:
						Commander-in-Chief - DOC, GEN5
						Commanding Officer - Constance, CPT
						Executive Officer - Deceded, LTC
						BetaHook, PFC - On Leave
						Guthrie, PFC - On Leave
						Constance, CPT
						Juvenis, COL
					*/

					var person = await GetPersonFromName(data, name, rank);
					if (onLeave) person.Status = "on leave";

					var personToUnit = data.PersonUnits.FirstOrDefault(p => p.Person.PersonId == person.PersonId && p.Unit.UnitId == unitId);
					if (personToUnit == null)
					{
						personToUnit = new PersonUnit();
						personToUnit.Person = person;
						personToUnit.Unit = data.Units.Find(unitId);
						personToUnit.Joined = DateTime.UtcNow;
						personToUnit = data.PersonUnits.Add(personToUnit);
					}
					personToUnit.PositionNameShort = positionNameShort;
					personToUnit.Removed = new DateTime(9999, 1, 1); // (DateTime)SqlDateTime.MaxValue;

					PersonToUnitId = personToUnit.PersonUnitId;
					try
					{
						await data.SaveChangesAsync();
					}
					catch (Exception e)
					{
						log.Fatal(e);
					}
				}
			}

			async Task ParseUnitContents(ILogEnd log, HtmlNode unitNamePlusUl, long? parentUnitId)
			{
				List<Task> tasks;
				using (var data = db.NewContext)
				{
					var unitTypeNameElement = unitNamePlusUl.SelectSingleNode("li | span");
					var unitTypeA = unitTypeNameElement.SelectSingleNode("*/a[1] | a[1]");
					var unitNameA = unitTypeNameElement.SelectSingleNode("*/a[2] | a[2]");

					var type = unitTypeA.InnerText;
					var tawId = int.Parse(unitNameA.GetAttributeValue("href", "/unit/-1.aspx").TakeStringBetweenLast("/", ".aspx"));
					var name = unitNameA.InnerText;

					log.Trace("parsing " + tawId);

					var unit = await GetUnit(data, tawId, name);
					unit.Type = type;
					if (parentUnitId.HasValue) unit.ParentUnit = data.Units.Find(parentUnitId.Value);

					await data.SaveChangesAsync();

					var children = unitNamePlusUl.SelectSingleNode("ul");

					tasks = new List<Task>(children.ChildNodes.Count);

					foreach (var child in children.ChildNodes)
					{
						var personA = child.SelectSingleNode("a");
						if (personA != null)
						{
							// person
							var text = child.InnerText;
							//tasks.Add(Task.Run(() => ParsePersonFromUnitRoaster(text, unit.Id)));
							var personLine = new UnitRoasterPersonLine(log, text, unit.UnitId);

							lock (personNameToPersonLines)
							{
								List<UnitRoasterPersonLine> personLines;
								if (!personNameToPersonLines.TryGetValue(personLine.PersonName, out personLines))
								{
									personNameToPersonLines[personLine.PersonName] = personLines = new List<UnitRoasterPersonLine>();
								}
								personLines.Add(personLine);
							}
						}
						else
						{
							// unit
							tasks.Add(Task.Run(() => ParseUnitContents(log, child, unit.UnitId)));
						}
					}
				}
				await Task.WhenAll(tasks.ToArray());
			}
		}

		async static Task<Person> GetPersonFromName(MyDbContext data, string name, string rankNameShort)
		{
			var person = await GetPersonFromName(data, name);
			if (!rankNameShort.IsNullOrEmpty())
			{
				if (person.Ranks.Count == 0)
				{
					var personRank = new PersonRank();
					personRank.Person = person;
					personRank.NameShort = rankNameShort;
					personRank.ValidFrom = DateTime.UtcNow;
					person.Ranks.Add(personRank);
					await data.SaveChangesAsync();
				}
			}
			return person;
		}

		async static Task<Person> GetPersonFromName(MyDbContext data, string name)
		{
			if (name.IsNullOrWhiteSpace() || name.Length <= 1)
			{
				Debugger.Break();
			}

			var person = data.People.FirstOrDefault(p => p.Name == name);
			if (person == null)
			{
				person = new Person();
				person.Name = name;
				person = data.People.Add(person);
				try
				{
					await data.SaveChangesAsync();
				}
				catch
				{
					// conflicting key: entity was probably already added in different thread
					data.Entry(person).State = System.Data.Entity.EntityState.Detached; // detach errorenous entity, so context is valid
					person = data.People.First(p => p.Name == name);
				}
			}
			return person;
		}

		async static Task<Unit> GetUnit(MyDbContext data, int unitTawId, string name)
		{
			var unit = data.Units.FirstOrDefault(u => u.TawId == unitTawId);
			if (unit == null)
			{
				unit = new Unit();
				unit.TawId = unitTawId;
				unit = data.Units.Add(unit);
				try
				{
					await data.SaveChangesAsync();
				}
				catch (Exception e)
				{
					// conflicting key: entity was probably already added in different thread
					data.Entry(unit).State = System.Data.Entity.EntityState.Detached; // detach errorenous entity, so context is valid
					unit = data.Units.First(p => p.TawId == unitTawId);
				}
			}
			unit.Name = name;
			return unit;
		}

		class DossierMovements
		{
			public List<DossierMovement> Movements;

			public class DossierMovement
			{
				public string id;
				public string timestamp;
				public string description;
			}
		}

		public async Task UpdateInfoFromProfilePage(ILogEnd _log, string personName)
		{
			var log = _log.ProfileStart("updating profile of " + personName);

			var scope = log.Profile("getting html");
			var url = Person.GetPersonProfilePageUrl(personName);
			var response = await sessionManager.GetUrl(url, scope);
			var html = response.HtmlDocument;

			using (var data = db.NewContext)
			{
				var person = data.People.FirstOrDefault(p => p.Name == personName);
				if (person == null)
				{
					log.Error("person not found in database");
					return;
				}

				// steam profile id
				var steamProfileLinkPrefix = "http://steamcommunity.com/profiles/";
				var steamProfileLinkElement = html.GetElementbyId("hfSteam");
				if (steamProfileLinkElement != null)
				{
					var steamProfileLink = steamProfileLinkElement.GetAttributeValue("href", steamProfileLinkPrefix + "-1");
					var steamId = long.Parse(steamProfileLink.Substring(steamProfileLinkPrefix.Length));
					person.SteamId = steamId;
				}

				// avatar image
				var avatarElement = html.DocumentNode.SelectSingleNode("//*[@class='dossieravatar']/img");
				if (avatarElement != null)
				{
					var avatarImageLink = avatarElement.GetAttributeValue("src", null);
					if (avatarImageLink != null)
					{
						person.AvatarImageUrl = "http://taw.net" + avatarImageLink;
					}
				}

				// bio
				var biographyElement = html.DocumentNode.SelectSingleNode("//*[@id='dossierbio']");
				if (biographyElement != null)
				{
					var biography = biographyElement.InnerText.Trim();
					var bioTextHeader = "Bio:";
					if (biography.StartsWith(bioTextHeader)) biography = biography.Substring(bioTextHeader.Length);
					person.BiographyContents = biography;
				}

				var table = new HtmlTwoColsStringTable(html.DocumentNode.SelectNodes("//*[@class='dossiernexttopicture']/table//tr"));

				// country
				person.CountryName = table.GetValue("Location:", person.CountryName).Trim(); ;
				person.Status = table.GetValue("Status:", person.Status).Trim().ToLower();

				var joined = table.GetValue("Joined:", "01-01-0001");
				person.DateJoinedTaw = ParseUSDateTime(joined);

				person.LastProfileDataUpdatedDate = DateTime.UtcNow;

				await data.SaveChangesAsync();
			}

			// dossier movements
			// rank in time
			// position in unit in time
			{
				scope = log.Profile("getting movements");
				var res = await sessionManager.PostJsonAsync("http://taw.net/services/JSONFactory.asmx/GetMovement", new { callsign = personName }, scope);

				scope = log.ProfileStart("parsing movements");
				var d = (string)JObject.Parse(res)["d"];
				var dossierMovements = JsonConvert.DeserializeObject<DossierMovements>(d);

				using (var data = db.NewContext)
				{
					var person = data.People.FirstOrDefault(p => p.Name == personName);

					foreach (var dossierMovement in dossierMovements.Movements)
					{
						var timestamp = ParseUSDateTime(dossierMovement.timestamp);
						var tawId = long.Parse(dossierMovement.id);
						var description = dossierMovement.description;

						if (description.Contains("was admitted to TAW")) person.AdmittedToTaw = timestamp;
						else if (description.Contains("was promoted to") || description.Contains("applied for TAW"))
						{
							if (person.Ranks == null || person.Ranks.Any(r => r.TawId == 0))
							{
								while (person.Ranks?.Count > 0) data.PersonRanks.Remove(person.Ranks.First());
								person.Ranks = new List<PersonRank>();
							}

							if (!person.Ranks.Any(r => r.TawId == tawId))
							{
								string rankNameLong = "unknown";
								string byWho = null;

								if (description.Contains("applied for TAW"))
								{
									person.AppliedForTaw = timestamp;
									rankNameLong = "Recruit";
								}
								else
								{
									// aeroson was promoted to Sergeant by <a href="/member/Samblues.aspx">Samblues</a>.
									// aeroson was promoted to Private First Class by <a href="/member/MaverickSabre.aspx">MaverickSabre</a>.
									var rankByWho = description.TakeStringAfter("was promoted to").Trim();
									byWho = description.TakeStringAfter(" by ").TakeStringBetween(">", "</a>").Trim();
									while (byWho.EndsWith(".")) byWho = byWho.RemoveFromEnd(1).Trim();
									rankNameLong = rankByWho.TakeStringBefore("by").Trim();
								}

								var personRank = new PersonRank();
								personRank.NameLong = rankNameLong;
								personRank.ValidFrom = timestamp;
								personRank.Person = person;
								if (!byWho.IsNullOrWhiteSpace() && byWho.Length > 0) personRank.PromotedBy = await GetPersonFromName(data, byWho);
								personRank.TawId = tawId;
								person.Ranks.Add(personRank);
							}
						}
						else if (description.Contains("was joined to units"))
						{
							// aeroson was joined to units AM2 Charlie Squad by MaverickSabre.
							// aeroson was joined to units AM2 Charlie FT by Samblues.
							// <a href="/member/aeroson.aspx">aeroson</a> was joined to units <a href="/unit/3617.aspx">AM2 Charlie FT</a> by <a href="/member/Samblues.aspx">Samblues</a>.
						}
						else if (description.Contains("was removed from units"))
						{
							// aeroson was removed from units AM2 TI Office by MaverickSabre.
							// <a href="/member/aeroson.aspx">aeroson</a> was removed from units <a href="/unit/1549.aspx">AM2 TI Office</a> by <a href="/member/MaverickSabre.aspx">MaverickSabre</a>.
						}
						else if (description.Contains("was assigned to position"))
						{
							// aeroson was assigned to position Training Instructor in unit AM2 TI Office by MaverickSabre.
							// aeroson was assigned to position Squad Leader in unit AM2 Charlie Squad by MaverickSabre.
							// <a href="/member/aeroson.aspx">aeroson</a> was assigned to position Squad Leader in unit <a href="/unit/1505.aspx">AM2 Charlie Squad</a> by <a href="/member/MaverickSabre.aspx">MaverickSabre</a>.
						}
						else if (description.Contains("was removed from position"))
						{
							// aeroson was removed from position Training Instructor in unit AM2 TI Office by MaverickSabre.
							// <a href="/member/aeroson.aspx">aeroson</a> was removed from position Training Instructor in unit <a href="/unit/1549.aspx">AM2 TI Office</a> by MaverickSabre.
						}
						else if (description.Contains("was returned to active duty by"))
						{
							// <a href="/member/MaverickSabre.aspx">MaverickSabre</a> was returned to active duty by <a href="/member/Lucky.aspx">Lucky</a>.
						}
						else if (description.Contains("was put on leave by"))
						{
							// <a href="/member/MaverickSabre.aspx">MaverickSabre</a> was put on leave by <a href="/member/Juvenis.aspx">Juvenis</a>.
						}
						else if (description.Contains("was reinstated by"))
						{
							// <a href="/member/Dackey.aspx">Dackey</a> was reinstated by <a href="/member/Phenom.aspx">Phenom</a>
						}
						else if (description.Contains("was discharged by"))
						{
							// http://taw.net/member/gravedigger.aspx
							// leave from unit that is before this
							// <a href="/member/MaverickSabre.aspx">MaverickSabre</a> was discharged by <a href="/member/Lucid.aspx">Lucid</a>.
						}
						else if (description.Contains("was discharged honorable by"))
						{
							// <a href="/member/Xsage.aspx">Xsage</a> was discharged honorable by <a href="/member/TexasHillbilly.aspx">TexasHillbilly</a>.
						}
						else if (description.Contains("was discharged dishonorable by"))
						{
							// <a href="/member/Dackey.aspx">Dackey</a> was discharged dishonorable by <a href="/member/Juvenis.aspx">Juvenis</a>.
						}
						else if (description.Contains("Unknown was removed from unit Unknown by"))
						{
							// removed person from removes unit
						}
						else
						{
							scope.Warn("unexpected dossier row: " + description);
						}
					}
					await data.SaveChangesAsync();
				}
				scope.End();
			}

			log.End("done, parsed and saved");
		}

		public readonly static string[] possibleDateTimeFormats = new string[] {
			"M.d.yyyy H:m:s",
			"M.d.yyyy H:m",
			"M.d.yyyy",
		};

		public static DateTime ParseUSDateTime(string str)
		{
			str = str.Replace('/', '.');
			str = str.Replace('-', '.');

			TimeSpan timeZoneOffset = TimeSpan.Zero;

			var timeZoneStart = str.LastIndexOf("+");
			if (timeZoneStart == -1) timeZoneStart = str.LastIndexOf("-");
			if (timeZoneStart != -1)
			{
				var timeZoneStr = str.RemoveFromBegin(timeZoneStart + 1);
				try
				{
					var t = DateTime.ParseExact(timeZoneStr, "H:m", CultureInfo.InvariantCulture, DateTimeStyles.None).TimeOfDay;
					if (str[timeZoneStart] == '+') timeZoneOffset = t;
					else timeZoneOffset = -t;
				}
				catch (Exception e)
				{
					throw new Exception($"failed to {nameof(ParseUSDateTime)} {nameof(timeZoneOffset)}:'{timeZoneOffset}', error:{e}");
				}
				str = str.TakeFromBegin(timeZoneStart).Trim();
			}

			try
			{
				var d = DateTime.ParseExact(str, possibleDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None);
				d = d - timeZoneOffset;
				return d;
			}
			catch (Exception e)
			{
				throw new Exception($"failed to {nameof(ParseUSDateTime)} DateTime:'{str}', error:{e}");
				throw;
			}
		}

		public enum ParseEventResult
		{
			ValidEvent,
			ErrorenousEvent,
			BaseEvent,
			InvalidUriShouldRetry,
		}

		public async Task<ParseEventResult> ParseEventData(ILogEnd _log, long eventTawId)
		{
			var log = _log.ProfileStart("working event data " + eventTawId);
			try
			{
				var url = Event.GetEventPage(eventTawId);
				var scope = log.ScopeStart("getting html");
				var response = await sessionManager.GetUrl(url);
				scope.End();

				ParseEventResult result;

				scope = log.ScopeStart("parsing html");
				using (var data = db.NewContext)
				{
					result = await ParseEventData_1(scope, data, response, eventTawId);
				}
				scope.End();
				return result;
			}
			catch (Exception e)
			{
				log.Error("error");
				log.FatalException(e);
				return ParseEventResult.ErrorenousEvent;
			}
			finally
			{
				log.End();
			}
		}

		async Task<ParseEventResult> ParseEventData_1(ILogEnd log, MyDbContext data, MyHttpWebResponse response, long eventTawId)
		{
			var uriPath = response.ResponseUri.AbsolutePath;
			if (uriPath.Contains("event") == false)
			{
				log.Warn("the event you are trying to parse has invalid uri:" + uriPath + " should contain taw event id:" + eventTawId);
				return ParseEventResult.InvalidUriShouldRetry;
			}

			var eventTawIdStr = uriPath.Split('/', '\\').Last().RemoveFromEnd(".aspx".Length);
			var parsedEventTawId = int.Parse(eventTawIdStr);
			if (eventTawId != parsedEventTawId)
			{
				throw new Exception($"should not happen, looking for tat event id:{eventTawId} but found:{parsedEventTawId}");
			}

			var htmlText = response.ResponseText;
			htmlText = htmlText?.TakeStringAfter("ctl00_ctl00_bcr_bcr_UpdatePanel\">");
			if (htmlText.Contains("This is a Base Event and should never be seen"))
			{
				log.Trace("invalid 'base event', skipping");
				return ParseEventResult.BaseEvent; // http://taw.net/event/65132.aspx
			}

			var evt = data.Events.FirstOrDefault(e => e.TawId == eventTawId);
			if (evt == null)
			{
				evt = new Event();
				evt.TawId = eventTawId;
				evt = data.Events.Add(evt);
				await data.SaveChangesAsync();
			}
			await ParseEventData_2(log, data, evt, htmlText, eventTawId);
			await data.SaveChangesAsync();
			return ParseEventResult.ValidEvent;
		}

		async Task ParseEventData_2(ILogEnd log, MyDbContext data, Event evt, string htmlText, long eventTawId)
		{
			// this page is so badly coded the HTML is invalid, chrome shows it correctly though, kudos to it
			// but HtmlAgilityPack just fails on it

			var eventInfoText = htmlText.TakeStringBetween("<table cellpadding=\"20\" cellspacing=\"5\">", "</table>");

			var eventInfoDoc = new HtmlDocument();
			eventInfoDoc.LoadHtml(eventInfoText);
			var eventInfo = new HtmlTwoColsStringTable(eventInfoDoc.DocumentNode);
			/*
			Name	GRAW Practice -- Saber Squad Thursday Night (NA-SA)
			Description	GRAW Practice -- Saber Squad Thursday Night (NA-SA)
			Type	Practice
			Unit	Ghost Recon
			When	From: 6/3/2016 04:00:00 +02:00 to: 6/3/2016 05:00:00 +02:00
			Mandatory	Yes
			Cancelled	No
			*/
			evt.Name = eventInfo["Name"];
			evt.Description = eventInfo["Description"];
			evt.Type = eventInfo["Type"];
			evt.Mandatory = eventInfo["Mandatory"] == "Yes";
			evt.Cancelled = eventInfo["Cancelled"] == "Yes";

			var when = eventInfo["When"];

			var strFrom = when.TakeStringBetween("from:", "to:", StringComparison.InvariantCultureIgnoreCase).Trim();
			if (strFrom != null) evt.From = ParseUSDateTime(strFrom);

			var strTo = when.TakeStringAfter("to:", StringComparison.InvariantCultureIgnoreCase).Trim();
			if (strTo != null) evt.To = ParseUSDateTime(strTo);

			var attendeesText = htmlText.TakeStringBetween("<table width=100%>", "</table>");
			var attendessDoc = new HtmlDocument();
			attendessDoc.LoadHtml(attendeesText);
			var attendeesTable = new HtmlTable(attendessDoc.DocumentNode);

			var newPersonEvents = new List<PersonEvent>();
			var personEvents = evt.Attended.ToList();

			foreach (var row in attendeesTable)
			{
				var name = row[0]?.InnerText?.Trim();
				var nameHref = row[0]?.SelectSingleNode("a")?.GetAttributeValue("href", ""); // http://taw.net/event/66327.aspx last row, unit name has no link
				if (nameHref != null && nameHref.StartsWith("/member"))
				{
					if (name.IsNullOrWhiteSpace())
					{
						// a deleted member attended event, so there is a row of event attendee with empty name
					}
					else
					{
						var person = await GetPersonFromName(data, name);

						var personToEvent = personEvents.FirstOrDefault(p => p.EventId == evt.EventId && p.PersonId == person.PersonId);

						if (personToEvent == null)
						{
							personToEvent = new PersonEvent();
							personToEvent.EventId = evt.EventId;
							personToEvent.PersonId = person.PersonId;

							personEvents.Add(personToEvent);
							newPersonEvents.Add(personToEvent);
						}

						var attendanceStr = row[1]?.InnerText?.Trim();
						AttendanceType attendanceType = AttendanceType.Unknown;
						if (attendanceStr != null && Enum.TryParse(attendanceStr.ToLowerInvariant(), true, out attendanceType)) personToEvent.AttendanceType = attendanceType;

						var timestampStr = row[2]?.InnerText?.Trim();
						if (!timestampStr.Contains("--")) personToEvent.TimeStamp = ParseUSDateTime(timestampStr);
					}
				}
				else if (nameHref != null && nameHref.StartsWith("/unit"))
				{
					var unitTawIdStr = nameHref.Split('/', '\\').Last().RemoveFromEnd(".aspx".Length);
					var unitTawId = int.Parse(unitTawIdStr);
					var unit = await GetUnit(data, unitTawId, name);
					evt.Units.Add(unit);
				}
				else if (nameHref == null)
				{
					// event with no unit
				}
				else
				{
					throw new Exception("something is wrong, found unexpected data, taw event id:" + eventTawId);
				}
			}
			newPersonEvents.ForEach(pe => data.PersonEvents.Add(pe));
		}
	}
}