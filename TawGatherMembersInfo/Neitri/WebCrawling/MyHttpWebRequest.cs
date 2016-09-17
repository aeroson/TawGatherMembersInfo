using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;
using Neitri;
using HtmlAgilityPack;
using Neitri.WebCrawling;

namespace Neitri.WebCrawling
{
	public class MyHttpWebRequest
	{
		public CookieContainer CookieContainer
		{
			get
			{
				return httpWebRequest.CookieContainer;
			}
			set
			{
				httpWebRequest.CookieContainer = value;
			}
		}

		public string Method
		{
			get
			{
				return httpWebRequest.Method;
			}
			set
			{
				httpWebRequest.Method = value;
			}
		}

		public string ContentType
		{
			get
			{
				return httpWebRequest.ContentType;
			}
			set
			{
				httpWebRequest.ContentType = value;
			}
		}

		public long ContentLength
		{
			get
			{
				return httpWebRequest.ContentLength;
			}
			set
			{
				httpWebRequest.ContentLength = value;
			}
		}


		HttpWebRequest httpWebRequest;
		static long totalRequestsMade = 0;
		static DateTime lastRequestMade;
		//static Queue<DateTime> timesOfRequestsMade = new Queue<DateTime>();

		//http://stackoverflow.com/questions/703272/could-not-establish-trust-relationship-for-ssl-tls-secure-channel-soap
		static MyHttpWebRequest()
		{
			ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);
			// trust sender
			ServicePointManager.ServerCertificateValidationCallback = ((sender, cert, chain, errors) => true);
			// validate cert by calling a function
			ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback((sender, cert, chain, errors) => true);
		}

		public static MyHttpWebRequest Create(string url)
		{
			var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			var myHttpWebRequest = new MyHttpWebRequest()
			{
				httpWebRequest = httpWebRequest
			};
			return myHttpWebRequest;
		}
		public MyHttpWebResponse GetResponse()
		{
			totalRequestsMade++;
			var now = DateTime.UtcNow;

			/*
            const float keepForSeconds = 10;
            while (timesOfRequestsMade.Count > 0 && (now - timesOfRequestsMade.Peek()).TotalSeconds > keepForSeconds) timesOfRequestsMade.Dequeue();
            var averagePerSecond = timesOfRequestsMade.Count / keepForSeconds;
            */

			var lastRequestMadeMilisecondsAgo = (int)((now - lastRequestMade).TotalMilliseconds);
			var lastRequestMadeMilisecondsAgo_limit = 2000;
			if (lastRequestMadeMilisecondsAgo > 0 && lastRequestMadeMilisecondsAgo < lastRequestMadeMilisecondsAgo_limit)
			{
				//System.Threading.Thread.Sleep(lastRequestMadeMilisecondsAgo_limit - lastRequestMadeMilisecondsAgo);
			}

			//Log.Info("totalRequestsMade:" + totalRequestsMade);

			//log.Info("totalRequestsMade:" + totalRequestsMade + " averageRequestsPerSecond:" + averagePerSecond);
			/*
            const float averagePerSecond_limit = 0.5f;
            if (averagePerSecond > averagePerSecond_limit)
            {
                var throttleMiliseconds = (int)((averagePerSecond - averagePerSecond_limit) * 1000f * 50f);
                log.Info("averageRequestsPerSecond above " + averagePerSecond_limit + " throttling for " + throttleMiliseconds + "ms");
                System.Threading.Thread.Sleep(throttleMiliseconds);
            }
            */

			now = DateTime.UtcNow;
			lastRequestMade = now;
			//timesOfRequestsMade.Enqueue(now);
			return new MyHttpWebResponse((HttpWebResponse)httpWebRequest.GetResponse());
		}
		public Stream GetRequestStream()
		{
			return httpWebRequest.GetRequestStream();
		}
	}
}
