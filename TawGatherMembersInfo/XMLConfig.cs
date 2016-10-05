using Neitri;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace TawGatherMembersInfo
{
	public class XMLConfig
	{
		Dictionary<string, XElement> rootData = new Dictionary<string, XElement>();

		string filePath;

		public XMLConfig LoadAppConfig()
		{
			LoadFile(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
			return this;
		}

		public XMLConfig Reload()
		{
			LoadFile(filePath);
			return this;
		}

		public XMLConfig LoadFile(string filePath)
		{
			this.filePath = filePath;

			rootData.Clear();

			if (File.Exists(filePath) == false) throw new FileNotFoundException("'" + filePath + "' does not exist");
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.ConformanceLevel = ConformanceLevel.Fragment;

			var root = XDocument.Load(filePath).Root;

			// load all values from xml config
			foreach (var e in root.Elements())
			{
				rootData[e.Name.LocalName.ToLower()] = e;
			}

			return this;
		}

		void InsertLines(IEnumerable<string> insertLines)
		{
			var currentLines = File.ReadAllLines(filePath).ToList();

			while (currentLines.Count > 2 && string.IsNullOrWhiteSpace(currentLines[currentLines.Count - 1])) currentLines.RemoveAt(currentLines.Count - 1);

			var lastLine = currentLines.Last();
			currentLines.RemoveAt(currentLines.Count - 1);

			currentLines.AddRange(insertLines);
			currentLines.Add("");

			currentLines.Add(lastLine);

			File.WriteAllLines(filePath, currentLines.ToArray());
			Reload();
		}

		public string EvaluateValue(string value)
		{
			// in all values, replace [varName] with its actual value
			for (int i = 0; i < 5; i++) // few iterations to propagate [varName]
			{
				foreach (var kvp in rootData)
				{
					var key = "[" + kvp.Key + "]";
					var val = kvp.Value.Value;
					value = value.Replace(key, val, StringComparison.InvariantCultureIgnoreCase);
				}
			}
			return value;
		}

		public IEnumerable<T> GetMany<T>(IEnumerable<T> defaultMany = null, [CallerMemberName] string name = "")
		{
			if (!rootData.ContainsKey(name.ToLower()))
			{
				if (defaultMany == null) defaultMany = Enumerable.Empty<T>();
				var lines = new List<string>();
				lines.Add($"\t<{name}>");
				lines.AddRange(defaultMany.Select(v => $"\t\t<v>{v.ToString()}</v>"));
				lines.Add($"\t</{name}>");
				InsertLines(lines);
			}

			return _GetMany<T>(name);
		}

		IEnumerable<T> _GetMany<T>(string name)
		{
			var val = rootData[name.ToLower()].Elements().Select(e => EvaluateValue(e.Value));
			var ret = val.Select(v => (T)Convert.ChangeType(v, typeof(T)));
			return ret;
		}

		public T GetOne<T>(T defaultValue = default(T), [CallerMemberName] string name = "")
		{
			if (!rootData.ContainsKey(name.ToLower()))
			{
				var val = defaultValue.ToString();
				var item = $"\t<{name}>{val}</{name}>";
				InsertLines(new string[] { item });
			}
			return _GetOne<T>(name);
		}

		T _GetOne<T>(string name)
		{
			var val = rootData[name.ToLower()].Value;
			val = EvaluateValue(val);
			var ret = (T)Convert.ChangeType(val, typeof(T));
			return ret;
		}
	}
}