using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace TawGatherMembersInfo
{




    public class HtmlTable : List<List<HtmlNode>>
    {
        public HtmlTable(HtmlNode tableNode)
        {
            foreach (var tr in tableNode.SelectNodes(tableNode.XPath + "/tr"))
            {
                var trData = new List<HtmlNode>();
                foreach (var td in tr.SelectNodes(tr.XPath + "/td"))
                {
                    trData.Add(td);
                }
                this.Add(trData);
            }
        }
    }

    public class HtmlTwoColsStringTable : Dictionary<string, string>
    {
        public HtmlTwoColsStringTable(HtmlNodeCollection tableRows)
        {
            if (tableRows == null) return;
            foreach (var tr in tableRows)
            {
                var trData = new List<HtmlNode>();
                foreach (var td in tr.SelectNodes(tr.XPath + "/td"))
                {
                    trData.Add(td);
                }
                if (trData.Count == 2)
                {
                    this[trData[0].InnerText.Trim()] = trData[1].InnerText.Trim();
                }
            }
        }
    }

}
