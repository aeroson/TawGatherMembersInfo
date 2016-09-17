using Neitri;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
		Thread thread;

		public void Join()
		{
			thread.Join();
		}

		public void Run()
		{
			thread = new Thread(ThreadMain);
			thread.Name = this.GetType().ToString();
			thread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			thread.Start();
		}

		public void Stop()
		{
			if (httpListener != null) httpListener.Stop();
			if (thread != null && thread.IsAlive) thread.Abort();
		}

		void ThreadMain()
		{
			var serverPort = config.HttpServerPort;

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
			HttpListenerContext context;
			while (Thread.CurrentThread.ThreadState == ThreadState.Running)
			{
				context = null;
				try
				{
					context = httpListener.GetContext();
					if (ProcessContext(context))
					{
						context.Response.StatusCode = (int)HttpStatusCode.OK;
					}
					else
					{
						context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					}
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
						Log.Info(context.Request.HttpMethod + " " + context.Request.RawUrl + " " + ((HttpStatusCode)context.Response.StatusCode).ToString());
						context.Response.Close();
					}
				}
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

		bool ProcessContext(HttpListenerContext context)
		{
			var p = new PerRequestHandler();
			return p.ProcessContext(context, config, db);
		}
	}
}