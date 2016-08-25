using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using Neitri;

namespace TawGatherMembersInfo
{
	public class HttpServerHandler
	{
		InstancesContainer instances;

		HttpListener httpListener;
		Thread thread;
		int serverPort;

		public HttpServerHandler(InstancesContainer instances, int port)
		{
			this.instances = instances;
			this.serverPort = port;
		}

		public void Run()
		{
			thread = new Thread(ThreadMain);
			thread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			thread.Start();
		}

		public void Stop()
		{
			if (httpListener != null) httpListener.Stop();
			if (thread != null && thread.IsAlive) thread.Abort();
		}


		void ThreadMain()
		{


			if (!HttpListener.IsSupported)
			{
				Log.Info("HttpListener is not supported");
				return;
			}
			try
			{
				httpListener = new HttpListener();
				httpListener.Prefixes.Add("http://*:" + serverPort + "/");
				httpListener.Start();
			}
			catch (Exception e)
			{
				Log.Info("Failed to start server listener on, " + serverPort + " reason, " + e);
				return;
			}
			HttpListenerContext context;
			while (Thread.CurrentThread.ThreadState == ThreadState.Running)
			{
				context = null;
				try
				{
					context = httpListener.GetContext();
					if (ProcessContext(context))
					{
						context.Response.StatusCode = (int)HttpStatusCode.OK;
					}
					else
					{
						context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					}
				}
				catch (Exception e)
				{
					if (context != null) context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
					Log.Info(e);
				}
				finally
				{
					if (context != null)
					{
						Log.Info(context.Request.HttpMethod + " " + context.Request.RawUrl + " " + ((HttpStatusCode)context.Response.StatusCode).ToString());
						context.Response.Close();
					}
				}
			}
		}


		class Params : Dictionary<string, string>
		{
			public Params(string url)
			{
				while (url.StartsWith("/")) url = url.Substring(1);
				while (url.StartsWith(@"\")) url = url.Substring(1);
				while (url.StartsWith("?")) url = url.Substring(1);

				foreach (var part in url.Split('&'))
				{
					var kvp = part.Split('=');
					if (kvp.Length == 2)
					{
						this[Uri.UnescapeDataString(kvp[0].Trim())] = Uri.UnescapeDataString(kvp[1].Trim());
					}
				}
			}
		}



		void Format_Table_Version_1(StreamWriter o, Unit rootUnit)
		{
			var persons = new HashSet<Person>();
			rootUnit.FillWithAllPersons(persons);

			o.WriteLine("<table>");

			o.WriteLine("<thead>");
			o.WriteLine("<tr>");

			o.WriteLine("<td>name</td>");
			o.WriteLine("<td>avatar</td>");
			o.WriteLine("<td>country</td>");
			o.WriteLine("<td>dateJoinedTaw.year</td>");
			o.WriteLine("<td>dateJoinedTaw.month</td>");
			o.WriteLine("<td>dateJoinedTaw.day</td>");
			o.WriteLine("<td>daysInTaw</td>");
			o.WriteLine("<td>rankImageBig</td>");
			o.WriteLine("<td>rankImageSmall</td>");
			o.WriteLine("<td>rankNameLong</td>");
			o.WriteLine("<td>rankNameShort</td>");
			o.WriteLine("<td>status</td>");

			o.WriteLine("</tr>");
			o.WriteLine("</thead>");

			o.WriteLine("<tbody>");
			foreach (var p in persons)
			{
				o.WriteLine("<tr>");

				o.WriteLine("<td>" + p.Name + "</td>");
				o.WriteLine("<td>" + p.AvatarImageUrl + "</td>");
				o.WriteLine("<td>" + p.CountryName + "</td>");
				o.WriteLine("<td>" + p.DateJoinedTaw.Year + "</td>");
				o.WriteLine("<td>" + p.DateJoinedTaw.Month + "</td>");
				o.WriteLine("<td>" + p.DateJoinedTaw.Day + "</td>");
				o.WriteLine("<td>" + p.DaysInTaw + "</td>");
				o.WriteLine("<td>" + p.RankImageBigUrl + "</td>");
				o.WriteLine("<td>" + p.RankImageSmallUrl + "</td>");
				o.WriteLine("<td>" + p.RankNameLong + "</td>");
				o.WriteLine("<td>" + p.RankNameShort + "</td>");
				o.WriteLine("<td>" + p.Status + "</td>");

				o.WriteLine("</tr>");
			}
			o.WriteLine("</tbody>");


			o.WriteLine("</table>");
		}


		void Format_Table_Version_2(StreamWriter o, Unit rootUnit)
		{
			var persons = new HashSet<Person>();
			rootUnit.FillWithAllPersons(persons);

			o.WriteLine("<table>");

			o.WriteLine("<thead>");
			o.WriteLine("<tr>");

			o.WriteLine("<td>name</td>");
			o.WriteLine("<td>avatarImageUrl</td>");
			o.WriteLine("<td>countryCodeIso3166</td>");
			o.WriteLine("<td>countryFlagImageUrl</td>");
			o.WriteLine("<td>countryName</td>");
			o.WriteLine("<td>dateJoinedTaw.Year</td>");
			o.WriteLine("<td>dateJoinedTaw.Month</td>");
			o.WriteLine("<td>dateJoinedTaw.Day</td>");
			o.WriteLine("<td>daysInTaw</td>");
			o.WriteLine("<td>mostImportantIngameUnit.name</td>");
			o.WriteLine("<td>mostImportantIngameUnit.type</td>");
			o.WriteLine("<td>mostImportantIngameUnit.tawId</td>");
			o.WriteLine("<td>mostImportantIngameUnitPositionNameLong</td>");
			o.WriteLine("<td>mostImportantIngameUnitPositionNameShort</td>");
			o.WriteLine("<td>rankImageBigUrl</td>");
			o.WriteLine("<td>rankImageSmallUrl</td>");
			o.WriteLine("<td>rankNameLong</td>");
			o.WriteLine("<td>rankNameShort</td>");
			o.WriteLine("<td>status</td>");
			o.WriteLine("<td>teamSpeakName</td>");
			o.WriteLine("<td>teamSpeakPositionNameLong</td>");
			o.WriteLine("<td>teamSpeakPositionNameShort</td>");
			o.WriteLine("<td>teamSpeakUnit.name</td>");
			o.WriteLine("<td>teamSpeakUnit.type</td>");
			o.WriteLine("<td>teamSpeakUnit.tawId</td>");

			o.WriteLine("</tr>");
			o.WriteLine("</thead>");

			o.WriteLine("<tbody>");
			foreach (var p in persons)
			{
				o.WriteLine("<tr>");

				o.WriteLine("<td>" + p.Name + "</td>");
				o.WriteLine("<td>" + p.AvatarImageUrl + "</td>");
				o.WriteLine("<td>" + p.CountryCodeIso3166 + "</td>");
				o.WriteLine("<td>" + p.CountryFlagImageUrl + "</td>");
				o.WriteLine("<td>" + p.CountryName + "</td>");
				o.WriteLine("<td>" + p.DateJoinedTaw.Year + "</td>");
				o.WriteLine("<td>" + p.DateJoinedTaw.Month + "</td>");
				o.WriteLine("<td>" + p.DateJoinedTaw.Day + "</td>");
				o.WriteLine("<td>" + p.DaysInTaw + "</td>");
				o.WriteLine("<td>" + p.MostImportantIngameUnit.name + "</td>");
				o.WriteLine("<td>" + p.MostImportantIngameUnit.type + "</td>");
				o.WriteLine("<td>" + p.MostImportantIngameUnit.id + "</td>");
				o.WriteLine("<td>" + p.MostImportantIngameUnitPositionNameLong + "</td>");
				o.WriteLine("<td>" + p.MostImportantIngameUnitPositionNameShort + "</td>");
				o.WriteLine("<td>" + p.RankImageBigUrl + "</td>");
				o.WriteLine("<td>" + p.RankImageSmallUrl + "</td>");
				o.WriteLine("<td>" + p.RankNameLong + "</td>");
				o.WriteLine("<td>" + p.RankNameShort + "</td>");
				o.WriteLine("<td>" + p.Status + "</td>");
				o.WriteLine("<td>" + p.TeamSpeakName + "</td>");
				o.WriteLine("<td>" + p.TeamSpeakUnitPositionNameLong + "</td>");
				o.WriteLine("<td>" + p.TeamSpeakUnitPositionNameShort + "</td>");
				o.WriteLine("<td>" + p.TeamSpeakUnit.name + "</td>");
				o.WriteLine("<td>" + p.TeamSpeakUnit.type + "</td>");
				o.WriteLine("<td>" + p.TeamSpeakUnit.id + "</td>");

				o.WriteLine("</tr>");
			}
			o.WriteLine("</tbody>");


			o.WriteLine("</table>");
		}


		void Format_Table_Version_3(StreamWriter o, HashSet<Person> personsSet, HashSet<string> fields, string orderBy)
		{
			IEnumerable<Person> persons = personsSet;
			if (orderBy == "id") persons = personsSet.OrderBy(p => p.Id);

			o.WriteLine("<table>");

			o.WriteLine("<thead>");
			o.WriteLine("<tr>");

			o.WriteLine("<td>name</td>");


			var showAll = fields.Contains("all");
			var avatarImageUrl = fields.Contains("avatarImageUrl") || showAll;
			var country = fields.Contains("country") || showAll;
			var dateJoinedTaw = fields.Contains("dateJoinedTaw") || showAll;
			var daysInTaw = fields.Contains("daysInTaw") || showAll;
			var id = fields.Contains("id") || showAll;
			var mostImportantIngameUnit = fields.Contains("mostImportantIngameUnit") || showAll;
			var rank = fields.Contains("rank") || showAll;
			var status = fields.Contains("status") || showAll;
			var teamSpeak = fields.Contains("teamSpeak") || showAll;


			if (avatarImageUrl) o.WriteLine("<td>avatarImageUrl</td>");
			if (country)
			{
				o.WriteLine("<td>countryCodeIso3166</td>");
				o.WriteLine("<td>countryFlagImageUrl</td>");
				o.WriteLine("<td>countryName</td>");
			}
			if (dateJoinedTaw)
			{
				o.WriteLine("<td>dateJoinedTaw.Year</td>");
				o.WriteLine("<td>dateJoinedTaw.Month</td>");
				o.WriteLine("<td>dateJoinedTaw.Day</td>");
			}
			if (daysInTaw) o.WriteLine("<td>daysInTaw</td>");
			if (id) o.WriteLine("<td>id</td>");
			if (mostImportantIngameUnit)
			{
				o.WriteLine("<td>mostImportantIngameUnit.name</td>");
				o.WriteLine("<td>mostImportantIngameUnit.type</td>");
				o.WriteLine("<td>mostImportantIngameUnit.id</td>");
				o.WriteLine("<td>mostImportantIngameUnitPositionNameLong</td>");
				o.WriteLine("<td>mostImportantIngameUnitPositionNameShort</td>");
			}
			if (rank)
			{
				o.WriteLine("<td>rankImageBigUrl</td>");
				o.WriteLine("<td>rankImageSmallUrl</td>");
				o.WriteLine("<td>rankNameLong</td>");
				o.WriteLine("<td>rankNameShort</td>");
			}
			if (status) o.WriteLine("<td>status</td>");
			if (teamSpeak)
			{
				o.WriteLine("<td>teamSpeakName</td>");
				o.WriteLine("<td>teamSpeakUnit.name</td>");
				o.WriteLine("<td>teamSpeakUnit.type</td>");
				o.WriteLine("<td>teamSpeakUnit.id</td>");
				o.WriteLine("<td>teamSpeakUnitPositionNameLong</td>");
				o.WriteLine("<td>teamSpeakUnitPositionNameShort</td>");
			}

			o.WriteLine("</tr>");
			o.WriteLine("</thead>");

			o.WriteLine("<tbody>");
			foreach (var p in persons)
			{
				o.WriteLine("<tr>");

				o.WriteLine("<td>" + p.Name + "</td>");
				if (avatarImageUrl) o.WriteLine("<td>" + p.AvatarImageUrl + "</td>");
				if (country)
				{
					o.WriteLine("<td>" + p.CountryCodeIso3166 + "</td>");
					o.WriteLine("<td>" + p.CountryFlagImageUrl + "</td>");
					o.WriteLine("<td>" + p.CountryName + "</td>");
				}
				if (dateJoinedTaw)
				{
					o.WriteLine("<td>" + p.DateJoinedTaw.Year + "</td>");
					o.WriteLine("<td>" + p.DateJoinedTaw.Month + "</td>");
					o.WriteLine("<td>" + p.DateJoinedTaw.Day + "</td>");
				}
				if (daysInTaw) o.WriteLine("<td>" + p.DaysInTaw + "</td>");
				if (id) o.WriteLine("<td>" + p.Id + "</td>");
				if (mostImportantIngameUnit)
				{
					o.WriteLine("<td>" + p.MostImportantIngameUnit.name + "</td>");
					o.WriteLine("<td>" + p.MostImportantIngameUnit.type + "</td>");
					o.WriteLine("<td>" + p.MostImportantIngameUnit.id + "</td>");
					o.WriteLine("<td>" + p.MostImportantIngameUnitPositionNameLong + "</td>");
					o.WriteLine("<td>" + p.MostImportantIngameUnitPositionNameShort + "</td>");
				}
				if (rank)
				{
					o.WriteLine("<td>" + p.RankImageBigUrl + "</td>");
					o.WriteLine("<td>" + p.RankImageSmallUrl + "</td>");
					o.WriteLine("<td>" + p.RankNameLong + "</td>");
					o.WriteLine("<td>" + p.RankNameShort + "</td>");
				}
				if (status) o.WriteLine("<td>" + p.Status + "</td>");
				if (teamSpeak)
				{
					o.WriteLine("<td>" + p.TeamSpeakName + "</td>");
					o.WriteLine("<td>" + p.TeamSpeakUnit.name + "</td>");
					o.WriteLine("<td>" + p.TeamSpeakUnit.type + "</td>");
					o.WriteLine("<td>" + p.TeamSpeakUnit.id + "</td>");
					o.WriteLine("<td>" + p.TeamSpeakUnitPositionNameLong + "</td>");
					o.WriteLine("<td>" + p.TeamSpeakUnitPositionNameShort + "</td>");
				}

				o.WriteLine("</tr>");
			}
			o.WriteLine("</tbody>");


			o.WriteLine("</table>");
		}


		bool ProcessContext(HttpListenerContext context)
		{
			var binaryStream = new MemoryStream();
			var o = new StreamWriter(binaryStream);

			string format = "json";
			var parameters = new Params(context.Request.RawUrl);
			var authToken = parameters.GetValue("auth", "none");

			var authTokens = instances.config.Root.Descendants("authenticationTokens").First().Elements().Select(e => e.Value).ToList();
			if (authTokens.Contains(authToken) == false)
			{
				o.WriteLine("{\n\terror:' bad auth token'\n}");
			}
			else
			{
				format = parameters.GetValue("format", "table");
				var version = parameters.GetValue("version", "3");
				var fields = new HashSet<string>(parameters.GetValue("fields", "name").Split(','));
				var orderBy = parameters.GetValue("orderBy", "none");
				var goodApiCall = true;

				if (parameters.GetValue("type", "distinct_person_list") == "distinct_person_list")
				{
					Unit rootUnit = null;

					var rootUnitRegexPattern = parameters.GetValue("rootUnitRegex", null);
					if (rootUnitRegexPattern != null)
					{
						var rootUnitRegex = new System.Text.RegularExpressions.Regex(rootUnitRegexPattern);
						rootUnit = instances.roaster.CurrentData.allUnits.First(u => rootUnitRegex.IsMatch(u.name));
					}
					else
					{
						var rootUnitId = int.Parse(parameters.GetValue("rootUnitId", "1"));
						rootUnit = instances.roaster.CurrentData.idToUnit.GetValue(rootUnitId, null);
					}


					if (rootUnit == null)
					{
						o.WriteLine("{\n\terror:'root unit not found'\n}");
					}
					else
					{
						if (format == "table" && version == "1")
						{
							Format_Table_Version_1(o, rootUnit);
							goodApiCall = true;
						}
						if (format == "table" && version == "2")
						{
							Format_Table_Version_2(o, rootUnit);
							goodApiCall = true;
						}
						if (format == "table" && version == "3")
						{
							var personsSet = new HashSet<Person>();
							if (rootUnit == null) personsSet = instances.roaster.CurrentData.allPersons;
							else rootUnit.FillWithAllPersons(personsSet);

							Format_Table_Version_3(o, personsSet, fields, orderBy);
							goodApiCall = true;
						}
					}

				}

				if (goodApiCall == false)
				{
					o.WriteLine("{\n\terror:'wrong api call, ask owner for more details'\n}");
				}
			}

			o.Flush();
			context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
			if (format == "json") context.Response.ContentType = "application/json";
			if (format == "table") context.Response.ContentType = "text/html";
			context.Response.ContentLength64 = binaryStream.Length;
			context.Response.OutputStream.Write(binaryStream.GetBuffer(), 0, (int)binaryStream.Length);
			context.Response.OutputStream.Close();

			return true;
		}


	}
}