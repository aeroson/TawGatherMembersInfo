using Microsoft.Win32;
using Neitri.WebCrawling;
using System;
using System.Net;

namespace TawGatherMembersInfo
{
	public partial class LoggedInSession
	{
		public CookieContainer cookieContainer { get; set; }

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

			var form = new WebFormHandler(loginPageUrl, loginForm, cookieContainer);
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
		/// GETs url, logs in if we are logged out.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public MyHttpWebResponse GetUrl(string url)
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