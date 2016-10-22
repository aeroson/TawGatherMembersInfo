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

			void WriteHtmlTable(StringTable d, StreamWriter o)
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

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy;

			IEnumerable<IPropertyDescriptor> GetAllProperties()
			{
				return PropertyDescriptorUtils.GetAll(typeof(Person), flags).Where(p => RemoveProperty(p) == false);
			}

			bool RemoveProperty(IPropertyDescriptor p)
			{
				if (p == null) return true;
				if (p.IsDefined<NoApi>()) return true;
				// we dont want many to many realationship tables
				if (typeof(ICollection<>).IsAssignableFrom(p.Type)) return true;
				if (typeof(ICollection).IsAssignableFrom(p.Type)) return true;
				return false;
			}

			IPropertyDescriptor GetOneProperty(string name)
			{
				var p = PropertyDescriptorUtils.GetOne(typeof(Person), name, flags);
				if (RemoveProperty(p)) return null;
				return p;
			}

			StringTable Format_Table_Version_3(Unit rootUnit, IEnumerable<string> fields, string orderBy)
			{
				var fieldsToLower = fields.Select(s => s.ToLowerInvariant()).ToList();

				IEnumerable<IPropertyDescriptor> selectedProperties;

				if (fieldsToLower.Count == 1 && (fieldsToLower.Contains("*") || fieldsToLower.Contains("all")))
				{
					selectedProperties = GetAllProperties();
					fields = selectedProperties.Select(p => p.Name);
				}
				else
				{
					var s = new List<IPropertyDescriptor>();
					selectedProperties = s;

					foreach (var f in fieldsToLower)
					{
						var prop = GetOneProperty(f);
						if (prop != null) s.Add(prop);
						else throw new Exception($"field {f} was not found in {typeof(Person)}, possible field values are: {GetAllProperties().Select(p => p.Name).Join(",")}");
					}
				}

				var data = new StringTable();
				foreach (var f in fields) data.AddColumn(f);

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