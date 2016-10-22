using Neitri;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TawGatherMembersInfo.Models;

namespace TawGatherMembersInfo
{
	public partial class HttpServerHandler
	{
		[Dependency]
		Config config;

		[Dependency]
		RoasterManager roaster;

		[Dependency]
		DbContextProvider db;

		HttpListener httpListener;

		Task mainTask;
		CancellationTokenSource cancel = new CancellationTokenSource();

		static ILogEnd Log => Program.Log;

		public void Join()
		{
			mainTask?.Wait();
		}

		public void Run()
		{
			mainTask = Task.Run(ThreadMain, cancel.Token);
		}

		public void Stop()
		{
			cancel?.Cancel();
		}

		Dictionary<string, DateTime> ipToLastRequest = new Dictionary<string, DateTime>();

		async Task ThreadMain()
		{
			var serverPort = config.HttpServerPort;

			var allowRequestFromSameIpEverySeconds = config.GetOne(5, "httpServerAllowRequestFromSameIpEverySeconds");

			if (!HttpListener.IsSupported)
			{
				Log.Info("HttpListener is not supported");
				return;
			}
			try
			{
				httpListener = new HttpListener();
				httpListener.Prefixes.Add("http://*:" + serverPort + "/");
				httpListener.Start();
			}
			catch (Exception e)
			{
				Log.Info("Failed to start server listener on, " + serverPort + " reason, " + e);
				return;
			}
			while (cancel.Token.IsCancellationRequested == false)
			{
				var context = await httpListener.GetContextAsync();

				var ip = context.Request.RemoteEndPoint.Address.ToString();
				DateTime lastRequestTime;
				if (ipToLastRequest.TryGetValue(ip, out lastRequestTime) && lastRequestTime.AddSeconds(allowRequestFromSameIpEverySeconds) > DateTime.UtcNow)
				{
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					context.Response.Close();
					continue;
				}
				ipToLastRequest[ip] = DateTime.UtcNow;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				Task.Run(async () =>
				{
					try
					{
						context.Response.StatusCode = (int)await ProcessContext(context);
					}
					catch (Exception e)
					{
						if (context != null) context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
						Log.Info(e);
					}
					finally
					{
						if (context != null)
						{
							Log.Info(ip + " " + context.Request.HttpMethod + " " + context.Request.RawUrl + " " + ((HttpStatusCode)context.Response.StatusCode).ToString());
							context.Response.Close();
						}
					}
				}, cancel.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			}
		}

		class Params : Dictionary<string, string>
		{
			public Params(string url)
			{
				while (url.StartsWith("/")) url = url.Substring(1);
				while (url.StartsWith(@"\")) url = url.Substring(1);
				while (url.StartsWith("?")) url = url.Substring(1);

				foreach (var part in url.Split('&'))
				{
					var kvp = part.Split('=');
					if (kvp.Length == 2)
					{
						this[Uri.UnescapeDataString(kvp[0].Trim())] = Uri.UnescapeDataString(kvp[1].Trim());
					}
				}
			}
		}

		async Task<HttpStatusCode> ProcessContext(HttpListenerContext context)
		{
			var p = new PerRequestHandler();
			return await p.ProcessContext(context, config, db);
		}
	}
}