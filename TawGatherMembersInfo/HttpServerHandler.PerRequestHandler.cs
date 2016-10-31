using Neitri;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public partial class HttpServerHandler
	{
		class PerRequestHandler
		{
			async public Task<HttpStatusCode> ProcessContext(HttpListenerContext context, Config config, DbContextProvider db)
			{
				var parameters = new Params(context.Request.RawUrl);
				var authToken = parameters.GetValue("auth", "none");
				var binaryStream = new MemoryStream();
				var o = new StreamWriter(binaryStream);
				var format = "json";
				var status = HttpStatusCode.OK;

				if (config.AuthenticationTokens.Contains(authToken) == false)
				{
					o.WriteLine("{\n\terror:' bad auth token'\n}");
				}
				else
				{
					using (var data = db.NewContext)
					{
						format = parameters.GetValue("format", "table");
						int version = 0;
						int.TryParse(parameters.GetValue("version", "3"), out version);

						var fields = parameters.GetValue("fields", "name").Split(',');
						var orderBy = parameters.GetValue("orderBy", "none");

						var type = parameters.GetValue("type", "distinct_person_list");
						if (type == "distinct_person_list")
						{
							Unit rootUnit = null;

							var unitTawId = int.Parse(parameters.GetValue("rootUnitId", "1"));
							rootUnit = data.Units.FirstOrDefault(u => u.TawId == unitTawId);

							if (rootUnit == null)
							{
								o.WriteLine("{\n\terror:'root unit not found'\n}");
							}
							else
							{
								const int minimalSupportedVetsion = 3;
								if (version < minimalSupportedVetsion)
								{
									o.WriteLine("{\n\terror:'wrong api call, you are using old version: " + version + ", minimal supported version is: " + minimalSupportedVetsion + "'\n}");
								}
								else if (format == "table" && version == 3)
								{
									try
									{
										var tableData = Format_Table_Version_3(rootUnit, fields, orderBy);
										WriteHtmlTable(tableData, o);
									}
									catch (Exception e)
									{
										Log.Error(e);
										o.WriteLine("{\n\terror:'" + e.Message + "'\n}");
									}
								}
							}
						}
						else
						{
							o.WriteLine("{\n\terror:'unsupported parameter type: " + type + ", valid values are: distinct_person_list'\n}");
						}
					}
				}

				o.Flush();
				context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
				if (format == "json") context.Response.ContentType = "application/json";
				if (format == "table") context.Response.ContentType = "text/html";
				context.Response.ContentLength64 = binaryStream.Length;
				await context.Response.OutputStream.WriteAsync(binaryStream.GetBuffer(), 0, (int)binaryStream.Length);
				context.Response.OutputStream.Close();

				return status;
			}

			public class StringTable
			{
				public List<string> Columns { get; set; } = new List<string>();
				public List<Row> Rows { get; set; } = new List<Row>();

				public class Row
				{
					public List<string> Values { get; set; }

					public void Add(string val)
					{
						Values.Add(val);
					}
				}

				public Row NewRow()
				{
					var row = new Row();
					row.Values = new List<string>(Columns.Count);
					return row;
				}

				public void AddColumn(string name)
				{
					Columns.Add(name);
				}

				public void AddRow(Row row)
				{
					if (row.Values.Count != Columns.Count) throw new Exception($"values.Length {row.Values.Count} != columns.Count {Columns.Count}");
					Rows.Add(row);
				}
			}

			static void WriteHtmlTable(StringTable d, StreamWriter o)
			{
				o.WriteLine("<table>");

				o.WriteLine("<thead>");
				o.WriteLine("<tr>");

				foreach (var c in d.Columns)
				{
					o.WriteLine("<td>" + c + "</td>");
				}

				o.WriteLine("</tr>");
				o.WriteLine("</thead>");

				o.WriteLine("<tbody>");
				foreach (var r in d.Rows)
				{
					o.WriteLine("<tr>");
					foreach (var v in r.Values)
					{
						o.WriteLine("<td>" + v + "</td>");
					}
					o.WriteLine("</tr>");
				}
				o.WriteLine("</tbody>");

				o.WriteLine("</table>");
			}

			const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy;

			readonly static List<IPropertyDescriptor> allProperties;
			readonly static Dictionary<string, IPropertyDescriptor> lowerCaseNameToProperty;
			readonly static string allPropertiesNames;

			static PerRequestHandler()
			{
				allProperties = PropertyDescriptorUtils.GetAll(
					typeof(Person),
					ShouldExpandSequence,
					bindingFlags
				)
				.Where(IsValid)
				.ToList();

				lowerCaseNameToProperty = allProperties.ToDictionary(
					p => p.Name.ToLowerInvariant(),
					p => p
				);

				allPropertiesNames = allProperties.Select(p => p.Name).Join("\n");
			}

			static bool ShouldExpandSequence(IPropertyDescriptor property, int currentDepth)
			{
				if (currentDepth > 2) return false;
				if (property.Type == typeof(DateTime)) return true;
				if (property.Type.Assembly == Assembly.GetExecutingAssembly() && IsValid(property)) return true;
				return false;
			}

			static bool IsValid(IPropertyDescriptor p)
			{
				if (p == null) return false;
				if (p.IsDefined<NoApi>()) return false;
				// we dont want many to many realationship tables, it didnt work anyway, lets be explicit with NoApi
				//if (typeof(ICollection<>).IsAssignableFrom(p.Type)) return false;
				//if (typeof(ICollection).IsAssignableFrom(p.Type)) return false;
				return true;
			}

			static IPropertyDescriptor GetOneProperty(string name)
			{
				return lowerCaseNameToProperty.GetValue(name, null);
			}

			static StringTable Format_Table_Version_3(Unit rootUnit, string[] fields, string orderBy)
			{
				var fieldsToLower = fields.Select(s => s.ToLowerInvariant()).ToList();
				IEnumerable<string> fieldNames = fields;

				List<IPropertyDescriptor> selectedProperties;

				if (fieldsToLower.Contains("*") || fieldsToLower.Contains("all"))
				{
					if (fieldsToLower.Count > 1) selectedProperties = lowerCaseNameToProperty.OrderBy(kvp => fieldsToLower.IndexOf(kvp.Key)).Select(kvp => kvp.Value).ToList();
					else selectedProperties = allProperties;
					fieldNames = selectedProperties.Select(p => p.Name);
				}
				else
				{
					selectedProperties = new List<IPropertyDescriptor>();
					for (int i = 0; i < fields.Length; i++)
					{
						var prop = GetOneProperty(fieldsToLower[i]);
						if (prop != null) selectedProperties.Add(prop);
						else throw new Exception($"field {fields[i]} was not found in {typeof(Person)}, possible field values are:\n{allPropertiesNames}");
					}
				}

				var data = new StringTable();
				foreach (var f in fieldNames) data.AddColumn(f);

				var people = rootUnit.GetAllActivePeople();

				IEnumerable<Person> persons = people;
				if (orderBy == "id") persons = people.OrderBy(p => p.PersonId);

				foreach (var person in persons)
				{
					var row = data.NewRow();

					foreach (var p in selectedProperties)
					{
						var val = p.Read(person);
						row.Add(val == null ? string.Empty : val.ToString());
					}

					data.AddRow(row);
				}

				return data;
			}
		}
	}
}