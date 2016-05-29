using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;

namespace TawGatherMembersInfo
{
    public static class ExtensionMethods
    {

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            TValue ret;
            if (dictionary.TryGetValue(key, out ret) == false) return defaultValue;
            return ret;
        }

        public static string StreamReadTextToEnd(this Stream s)
        {
            using (var sr = new StreamReader(s))
            {
                return sr.ReadToEnd();
            }
        }

        public static HtmlDocument HtmlStringToDocument(this string htmlText)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlText);
            return html;
        }


    }
}
