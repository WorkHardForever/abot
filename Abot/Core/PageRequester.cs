using System;
using System.Net;
using System.Threading.Tasks;
using Abot.Poco;
using Abot.Util.Time;
using log4net;

namespace Abot.Core
{
	/// <summary>
	/// Work with requesting pages and getting its contents
	/// </summary>
	[Serializable]
	public class PageRequester : IPageRequester
	{
		#region Const
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		protected const string CAcceptRequest = "*/*";

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		#endregion

		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		/// <summary>
		/// Config with all options to get page or miss it
		/// </summary>
		protected CrawlConfiguration Config;


		protected IWebContentExtractor Extractor;

		/// <summary>
		/// Cookie of responsed page
		/// </summary>
		protected CookieContainer CookieContainer = new CookieContainer();

		#endregion

		#region Ctor

		/// <summary>
		/// Set received config
		/// </summary>
		/// <param name="config"></param>
		public PageRequester(CrawlConfiguration config)
			: this(config, null)
		{ }

		/// <summary>
		/// Set received config
		/// </summary>
		/// <param name="config"></param>
		/// <param name="contentExtractor"></param>
		public PageRequester(CrawlConfiguration config, IWebContentExtractor contentExtractor)
		{
			Config = config ?? throw new ArgumentNullException(nameof(config));
			Extractor = contentExtractor ?? new WebContentExtractor();

			// Set ServicePointManager credentials
			if (Config.HttpServicePointConnectionLimit > 0)
				ServicePointManager.DefaultConnectionLimit = Config.HttpServicePointConnectionLimit;

			if (!Config.IsSslCertificateValidationEnabled)
				ServicePointManager.ServerCertificateValidationCallback +=
					(sender, certificate, chain, sslPolicyErrors) => true;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Make an http web request to the url and download its content
		/// </summary>
		public virtual Task<CrawledPage> MakeRequestAsync(Uri uri) =>
            MakeRequestAsync(uri, x => new CrawlDecision { Allow = true });

		/// <summary>
		/// Make an http web request to the url and download its content based on the param func decision
		/// </summary>
		public virtual async Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));

			// Create page for crawling
			CrawledPage crawledPage = new CrawledPage(uri);

			// Sending request and getting response 
			HttpWebRequest request = null;
			HttpWebResponse response = null;

			try
			{
				request = BuildRequestObject(uri);

				crawledPage.RequestStarted = DateTime.Now;
				response = await request.GetResponseAsync() as HttpWebResponse;

				ProcessResponseObject(response);
			}
			catch (WebException e)
			{
				crawledPage.WebException = e;

				if (e.Response != null)
					response = (HttpWebResponse)e.Response;

				Logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
				Logger.Debug(e);
			}
			catch (Exception e)
			{
				Logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
				Logger.Debug(e);
			}
			finally
			{
				try
				{
					crawledPage.HttpWebRequest = request;
					crawledPage.RequestCompleted = DateTime.Now;

					if (response != null)
					{
						crawledPage.HttpWebResponse = new HttpWebResponseWrapper(response);

						CrawlDecision shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
						if (shouldDownloadContentDecision.Allow)
						{
							crawledPage.DownloadContentStarted = DateTime.Now;
							// Collect useful info from page
							crawledPage.Content = Extractor.GetContent(response);
							crawledPage.DownloadContentCompleted = DateTime.Now;
						}
						else
						{
							Logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
						}

						// Should already be closed by _extractor but just being safe
						response.Close();
					}
				}
				catch (Exception e)
				{
					Logger.DebugFormat("Error occurred finalizing requesting url [{0}]", uri.AbsoluteUri);
					Logger.Debug(e);
				}
			}

			return crawledPage;
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			CookieContainer = null;
			Config = null;
		}

		#endregion

		#region Protected Method

		/// <summary>
		/// Create request by config settings
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		protected virtual HttpWebRequest BuildRequestObject(Uri uri)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

			request.AllowAutoRedirect = Config.IsHttpRequestAutoRedirectsEnabled;
			request.UserAgent = Config.UserAgentString;
			request.Accept = CAcceptRequest;

			if (Config.HttpRequestMaxAutoRedirects > 0)
				request.MaximumAutomaticRedirections = Config.HttpRequestMaxAutoRedirects;

			if (Config.IsHttpRequestAutomaticDecompressionEnabled)
				request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			if (Config.HttpRequestTimeoutInSeconds > 0)
				request.Timeout = (int)TimeConverter.SecondsToMilliseconds(Config.HttpRequestTimeoutInSeconds);

			if (Config.IsSendingCookiesEnabled)
				request.CookieContainer = CookieContainer;

			//Supposedly this does not work... https://github.com/sjdirect/abot/issues/122
			//if (_config.IsAlwaysLogin)
			//{
			//    request.Credentials = new NetworkCredential(_config.LoginUser, _config.LoginPassword);
			//    request.UseDefaultCredentials = false;
			//}
			if (Config.IsAlwaysLogin)
			{
				string credentials = Convert.ToBase64String(
					System.Text.Encoding.ASCII.GetBytes(Config.LoginUser + ":" + Config.LoginPassword)
				);

				request.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
			}

			return request;
		}

		/// <summary>
		/// Process after receiving response by config credentials
		/// </summary>
		/// <param name="response"></param>
		protected virtual void ProcessResponseObject(HttpWebResponse response)
		{
			if (response != null && Config.IsSendingCookiesEnabled)
			{
				CookieCollection cookies = response.Cookies;
				CookieContainer.Add(cookies);
			}
		}

		///// <summary>
		///// Asynchronously make an http web request to the url and download its content based on the param func decision
		///// </summary>
		//public Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
		//{
		//    if (uri == null)
		//        throw new ArgumentNullException("uri");

		//    CrawledPage crawledPage = new CrawledPage(uri);
		//    crawledPage.RequestStarted = DateTime.Now;

		//    HttpWebRequest request = BuildRequestObject(uri);
		//    HttpWebResponse response = null;

		//    crawledPage.HttpWebRequest = request;
		//    crawledPage.RequestStarted = DateTime.Now;

		//    Task<WebResponse> task = Task.Factory.FromAsync(
		//        request.BeginGetResponse,
		//        asyncResult => request.EndGetResponse(asyncResult),
		//        null);

		//    return task.ContinueWith((Task<WebResponse> t) =>
		//    {
		//        crawledPage.RequestCompleted = DateTime.Now;

		//        if (t.IsFaulted)
		//        {
		//            //handle error
		//            Exception firstException = t.Exception.InnerExceptions.First();
		//            crawledPage.WebException = firstException as WebException;

		//            if (crawledPage.WebException != null && crawledPage.WebException.Response != null)
		//                response = (HttpWebResponse)crawledPage.WebException.Response;

		//            _logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
		//            _logger.Debug(crawledPage.WebException);
		//        }
		//        else
		//        {
		//            ProcessResponseObject(response);
		//            response = (HttpWebResponse)t.Result;
		//        }

		//        if (response != null)
		//        {
		//            crawledPage.HttpWebResponse = response;
		//            CrawlDecision shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
		//            if (shouldDownloadContentDecision.Allow)
		//            {
		//                crawledPage.DownloadContentStarted = DateTime.Now;
		//                crawledPage.Content = _extractor.GetContent(response);
		//                crawledPage.DownloadContentCompleted = DateTime.Now;
		//            }
		//            else
		//            {
		//                _logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri,
		//                    shouldDownloadContentDecision.Reason);
		//            }

		//            response.Close(); //Should already be closed by _extractor but just being safe
		//        }

		//        return crawledPage;
		//    });
		//}

		#endregion
	}
}
