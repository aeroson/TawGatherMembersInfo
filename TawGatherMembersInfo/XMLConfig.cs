using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TawGatherMembersInfo
{
	public class XMLConfig : Dictionary<string, string>
	{
		public XElement Root { get; set; }
		public HashSet<string> authTokens = new HashSet<string>();

		public void LoadAppConfig()
		{
			LoadFile(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
		}

		public void LoadFile(string filePath)
		{
			if (File.Exists(filePath) == false) throw new FileNotFoundException("'" + filePath + "' does not exist");
			this.Root = XDocument.Load(filePath).Root;

			// load all values from xml config
			foreach (var e in Root.Elements())
			{
				this[e.Name.LocalName] = e.Value;
			}

			// in all values, replace [varName] with its actual value
			for (int i = 0; i < 5; i++) // few iterations to propagate [varName]
			{
				var valuesCopy = new Dictionary<string, string>(this);
				foreach (var kvp_replaceInThis in valuesCopy.Keys)
				{
					foreach (var kvp_replaceBy in valuesCopy)
					{
						this[kvp_replaceInThis] = this[kvp_replaceInThis].Replace("[" + kvp_replaceBy.Key + "]", kvp_replaceBy.Value);
					}
				}
			}

			authTokens = new HashSet<string>(
				Root.Descendants("authenticationTokens").First().Elements().Select(e => e.Value)
			);
		}
	}
}