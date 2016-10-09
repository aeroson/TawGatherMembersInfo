using Microsoft.Win32;
using Neitri;
using Neitri.WebCrawling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TawGatherMembersInfo
{
	/// <summary>
	/// Single thread.
	/// </summary>
	public partial class LoggedInSession
	{
		static ILogEnd Log => Program.Log;

		public int MaxRequestsPerMinute { get; set; }

		public CookieContainer CookieContainer { get; set; }

		string loginPageUrl = @"https://taw.net/themes/taw/common/login.aspx";

		string username;
		string password;

		public LoggedInSession()
		{
			CookieContainer = new CookieContainer();

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

		Queue<DateTime> requestTimes = new Queue<DateTime>();
		int throttleForSeconds = 1;

		public void ThrottleBeforeRequest()
		{
			if (MaxRequestsPerMinute <= 0) return;
			do
			{
				var removeIfUnder = DateTime.UtcNow.Subtract(new TimeSpan(0, 1, 0));
				while (requestTimes.Count > 0 && requestTimes.Peek() < removeIfUnder) requestTimes.Dequeue();
				if (requestTimes.Count > MaxRequestsPerMinute)
				{
					Log.Trace($"too many requests per minute: {requestTimes.Count}, over limit: {MaxRequestsPerMinute}, sleeping for: {throttleForSeconds} seconds");
					Thread.Sleep(throttleForSeconds * 1000);
					throttleForSeconds *= 2;
				}
			}
			while (requestTimes.Count > MaxRequestsPerMinute);
			requestTimes.Enqueue(DateTime.UtcNow);
			throttleForSeconds = 1;
		}

		public async Task ThrottleBeforeRequestAsync()
		{
			if (MaxRequestsPerMinute <= 0) return;
			do
			{
				var removeIfUnder = DateTime.UtcNow.Subtract(new TimeSpan(0, 1, 0));
				while (requestTimes.Count > 0 && requestTimes.Peek() < removeIfUnder) requestTimes.Dequeue();
				if (requestTimes.Count > MaxRequestsPerMinute)
				{
					Log.Trace($"too many requests per minute: {requestTimes.Count}, over limit: {MaxRequestsPerMinute}, sleeping for: {throttleForSeconds} seconds");
					await Task.Delay(throttleForSeconds * 1000);
					throttleForSeconds *= 2;
				}
			}
			while (requestTimes.Count > MaxRequestsPerMinute);
			requestTimes.Enqueue(DateTime.UtcNow);
			throttleForSeconds = 1;
		}

		/// <summary>
		/// Login to the website with credentials.
		/// </summary>
		async Task Login()
		{
			Log.Trace("Logging in...");

			var request = MyHttpWebRequest.Create(loginPageUrl);
			request.CookieContainer = CookieContainer;
			request.Method = "GET";

			await ThrottleBeforeRequestAsync();
			var response = await request.GetResponseAsync();

			var html = response.HtmlDocument;
			var loginForm = html.GetElementbyId("aspnetForm");

			var form = new WebFormHandler(loginPageUrl, loginForm, CookieContainer);
			form.FillInput("ctl00$bcr$ctl03$ctl07$username", username);
			form.FillInput("ctl00$bcr$ctl03$ctl07$password", password);
			form.FillInput("ctl00$bcr$ctl03$ctl07$loginButton", "Sign in »");
			response = form.SubmitForm();

			if (IsLoggedIn(response.ResponseText))
			{
				Log.Trace("Successfully logged in...");
			}
			else
			{
				Log.Warn("Failed to log in...");
				Log.Trace("Resetting registry stored user details.");
				var registry = Registry.CurrentUser.CreateSubKey(this.GetType().FullName);
				registry.SetValue("username", "");
				registry.SetValue("password", "");
			}
		}

		bool IsLoggedIn(string responseText)
		{
			return responseText.Contains(">Sign in </a>") == false;
		}

		/// <summary>
		/// GETs url, logs in if we are logged out.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public async Task<MyHttpWebResponse> GetUrl(string url)
		{
			MyHttpWebResponse response;
			string responseText = null;

			do
			{
				if (responseText != null) await Login();

				var request = MyHttpWebRequest.Create(url);
				request.CookieContainer = CookieContainer;
				request.Method = "GET";

				await ThrottleBeforeRequestAsync();
				response = await request.GetResponseAsync();
				responseText = response.ResponseText;
			} while (IsLoggedIn(responseText) == false);

			return response;
		}

		public async Task<string> PostJson(string url, object payload)
		{
			var requestText = JsonConvert.SerializeObject(payload);
			var requsstTextBytes = Encoding.UTF8.GetBytes(requestText);

			var request = MyHttpWebRequest.Create(url);
			request.CookieContainer = CookieContainer;
			request.Method = "POST";
			request.ContentType = "application/json";
			request.GetRequestStream().Write(requsstTextBytes, 0, requsstTextBytes.Length);

			await ThrottleBeforeRequestAsync();
			var response = await request.GetResponseAsync();
			var responseText = response.ResponseText;

			return responseText;
		}

		public void ClearCookies()
		{
			CookieContainer = new CookieContainer();
		}
	}

	public class TheadedVariableManager<T> where T : class, new()
	{
		SemaphoreSlim s;
		ConcurrentDictionary<Thread, T> threadToVar = new ConcurrentDictionary<Thread, T>();

		public class DisposableWrapper : IDisposable
		{
			T var;
			TheadedVariableManager<T> manager;
			public T Value => var;

			public DisposableWrapper(TheadedVariableManager<T> manager, T var)
			{
				this.var = var;
				this.manager = manager;
			}

			public void Dispose()
			{
				manager.s.Release();
			}

			public static implicit operator T(DisposableWrapper me)
			{
				return me.var;
			}
		}

		public TheadedVariableManager(int maxInstances)
		{
			s = new SemaphoreSlim(maxInstances);
		}

		public virtual async Task<DisposableWrapper> GetAsync()
		{
			await s.WaitAsync();
			T var;
			if (!threadToVar.TryGetValue(Thread.CurrentThread, out var)) var = threadToVar[Thread.CurrentThread] = new T();
			return new DisposableWrapper(this, var);
		}
	}

	public class SessionMannager : TheadedVariableManager<LoggedInSession>
	{
		int maxRequestsPerMinutePerSession;

		public SessionMannager(int maxInstances, int maxRequestsPerMinutePerSession) : base(maxInstances)
		{
			this.maxRequestsPerMinutePerSession = maxRequestsPerMinutePerSession;
		}

		public override async Task<DisposableWrapper> GetAsync()
		{
			var r = await base.GetAsync();
			r.Value.MaxRequestsPerMinute = maxRequestsPerMinutePerSession;
			return r;
		}

		public async Task<string> PostJsonAsync(string url, object payload, LogScope log = null)
		{
			if (log != null) log.Trace("waiting for session");
			using (var session = await GetAsync())
			{
				if (log != null) log.Start();
				var ret = await session.Value.PostJson(url, payload);
				if (log != null) log.End();
				return ret;
			}
		}

		public async Task<MyHttpWebResponse> GetUrl(string url, LogScope log = null)
		{
			if (log != null) log.Trace("waiting for session");
			using (var session = await GetAsync())
			{
				if (log != null) log.Start();
				var ret = await session.Value.GetUrl(url);
				if (log != null) log.End();
				return ret;
			}
		}
	}
}