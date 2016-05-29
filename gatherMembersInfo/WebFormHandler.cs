using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;
using System.Net;


namespace TawGatherMembersInfo
{
    public class WebFormHandler
    {

        public class ParamsCollection : Dictionary<string, string>
        {
            public string ToData()
            {
                return string.Join("&", this.Select(kvp => WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value)).ToArray());
            }
        }


        HtmlNode formHtmlElement;
        CookieContainer cookieContainer;
        ParamsCollection paramsCollection = new ParamsCollection();
        string currentPageUrl;

        public WebFormHandler(string currentPageUrl, HtmlNode formHtmlElement, HtmlDocument wholePage, CookieContainer cookieContainer)
        {
            this.currentPageUrl = currentPageUrl;
            this.formHtmlElement = formHtmlElement;
            this.cookieContainer = cookieContainer;

            foreach (var e in formHtmlElement.SelectNodes("//input[@type='hidden']|//input[@type='text']|//input[@type='password']|//input[@type='submit']"))
            {
                var name = e.GetAttributeValue("name", null);
                if (name != null)
                {
                    FillInput(name, e.GetAttributeValue("value", ""));
                }
            }

            //FillInput("ctl00_scriptManager_TSM", "");

            /* DONE should automatically fill all input type hidden text password submit
            foreach (var s in new string[] { "__LASTFOCUS", "__EVENTTARGET", "__EVENTARGUMENT", "__VIEWSTATE", "__EVENTVALIDATION" })
            {
                FillInput(s, wholePage.GetElementbyId(s).GetAttributeValue("value", ""));
            }*/
        }
        public void FillInput(string inputName, string inputValue)
        {
            paramsCollection[inputName] = inputValue;
        }
        public HttpWebResponse SubmitForm()
        {
            var url = formHtmlElement.GetAttributeValue("action", "?");
            if (url.StartsWith("/") == false && url.StartsWith(@"\") == false) url = "/" + url;
            url = currentPageUrl + url;

            var method = formHtmlElement.GetAttributeValue("method", "post").ToLower();

            if (method == "get")
            {
                url += "?" + paramsCollection.ToData();
            }

            var request = MyHttpWebRequest.Create(url);
            request.CookieContainer = cookieContainer;
            request.Method = method;
            request.ContentType = "application/x-www-form-urlencoded";

            if (method == "post")
            {
                var s = request.GetRequestStream();
                using (var t = new StreamWriter(s))
                {
                    t.Write(paramsCollection.ToData());
                }
            }
            if (method == "get")
            {
                request.ContentLength = 0;
            }
            
            var response = request.GetResponse();
            response.GetResponseStream();

            return response;
        }

    }
}