using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Web;

using HtmlAgilityPack;

using Microsoft.Win32;
using Neitri.WebCrawling;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{

    public partial class LoggedInSession
    {

        public RoasterData roaster = new RoasterData();

        public CookieContainer cookieContainer { get; private set; }



        string loginPageUrl = @"https://taw.net/themes/taw/common/login.aspx";

        string username;
        string password;

        public LoggedInSession()
        {
            cookieContainer = new CookieContainer();

            var registry = Registry.CurrentUser.CreateSubKey(this.GetType().FullName);
            username = registry.GetValue("username", "") as string;
            password = registry.GetValue("password", "") as string;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                var form = new EnterTawUserLoginInfo();
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    username = form.Username;
                    password = form.Password;
                    if (form.RememeberLoginDetails)
                    {
                        registry.SetValue("username", username);
                        registry.SetValue("password", password);
                    }
                }
                else
                {
                    throw new Exception("username or password are not present, unable to gather profile pages data");
                }
            }

        }



        /// <summary>
        /// Login to the website with credentials.
        /// </summary>
        public void Login()
        {

            Log.Info("Logging in...");

            var request = MyHttpWebRequest.Create(loginPageUrl);
            request.CookieContainer = cookieContainer;
            request.Method = "GET";

            var response = request.GetResponse();

            var html = response.HtmlDocument;
            var loginForm = html.GetElementbyId("aspnetForm");

            var form = new WebFormHandler(loginPageUrl, loginForm, html, cookieContainer);
            form.FillInput("ctl00$bcr$ctl03$ctl07$username", username);
            form.FillInput("ctl00$bcr$ctl03$ctl07$password", password);
            form.FillInput("ctl00$bcr$ctl03$ctl07$loginButton", "Sign in »");
            response = form.SubmitForm();

            if (IsLoggedIn(response.ResponseText))
            {
                Log.Info("Successfully logged in...");
            }
            else
            {
                Log.Info("Failed to log in...");
                Log.Info("Resetting registry stored user details.");
                var registry = Registry.CurrentUser.CreateSubKey(this.GetType().FullName);
                registry.SetValue("username", "");
                registry.SetValue("password", "");
            }
        }


        public bool IsLoggedIn(string responseText)
        {
            return responseText.Contains(">Sign in </a>") == false;
        }

        /// <summary>
        /// Gather basic information from unit roaster, still needs more detailed updating for each person from his/her profile page.
        /// </summary>
        public void GatherBasicInformationFromUnitId1Roaster()
        {
            Log.Enter();

            roaster.ClearUnitToPersonRelations();

            var url = Unit.GetUnitRoasterPage(1);
            var response = GetUrl(url);
            var html = response.HtmlDocument;

            var roasterDiv = html.GetElementbyId("ctl00_bcr_UpdatePanel1");

            roaster.rootUnit = roaster.CreateUnit(null, "TAW");
            roaster.rootUnit.ParseUnitContents(this, roasterDiv.SelectSingleNode(roasterDiv.XPath + "/div/ul/ul"));

            Log.Exit();
        }



        public void GatherEventData(int tawEventId)
        {
            Log.Info("gathering event id: " + tawEventId + " start");

            var url = Event.GetEventPage(tawEventId);
            var response = GetUrl(url);

            Event.ParseEventData(roaster, response);

            Log.Info("gathering event id: " + tawEventId + " end");
        }


        MyHttpWebResponse GetUrl(string url)
        {
            MyHttpWebResponse response;
            string responseText = null;

            do
            {
                if (responseText != null) Login();

                var request = MyHttpWebRequest.Create(url);
                request.CookieContainer = cookieContainer;
                request.Method = "GET";

                response = request.GetResponse();
                responseText = response.ResponseText;

            } while (IsLoggedIn(responseText) == false);

            return response;
        }

        public void ClearCookies()
        {
            cookieContainer = new CookieContainer();
        }



    }
}