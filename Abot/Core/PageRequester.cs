using System;
using System.Net;
using Abot.Poco;
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

		protected const string c_ACCEPT_REQUEST = "*/*";

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		#endregion

		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog _logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		/// <summary>
		/// Config with all options to get page or miss it
		/// </summary>
		protected CrawlConfiguration _config;


		protected IWebContentExtractor _extractor;

		/// <summary>
		/// Cookie of responsed page
		/// </summary>
		protected CookieContainer _cookieContainer = new CookieContainer();

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
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_extractor = contentExtractor ?? new WebContentExtractor();

			// Set ServicePointManager credentials
			if (_config.HttpServicePointConnectionLimit > 0)
				ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;

			if (!_config.IsSslCertificateValidationEnabled)
				ServicePointManager.ServerCertificateValidationCallback +=
					(sender, certificate, chain, sslPolicyErrors) => true;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Make an http web request to the url and download its content
		/// </summary>
		public virtual CrawledPage MakeRequest(Uri uri) => MakeRequest(uri, (x) => new CrawlDecision { Allow = true });

		/// <summary>
		/// Make an http web request to the url and download its content based on the param func decision
		/// </summary>
		public virtual CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
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
				response = (HttpWebResponse)request.GetResponse();

				ProcessResponseObject(response);
			}
			catch (WebException e)
			{
				crawledPage.WebException = e;

				if (e.Response != null)
					response = (HttpWebResponse)e.Response;

				_logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
				_logger.Debug(e);
			}
			catch (Exception e)
			{
				_logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
				_logger.Debug(e);
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
							crawledPage.Content = _extractor.GetContent(response);
							crawledPage.DownloadContentCompleted = DateTime.Now;
						}
						else
						{
							_logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
						}

						// Should already be closed by _extractor but just being safe
						response.Close();
					}
				}
				catch (Exception e)
				{
					_logger.DebugFormat("Error occurred finalizing requesting url [{0}]", uri.AbsoluteUri);
					_logger.Debug(e);
				}
			}

			return crawledPage;
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			_cookieContainer = null;
			_config = null;
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

			request.AllowAutoRedirect = _config.IsHttpRequestAutoRedirectsEnabled;
			request.UserAgent = _config.UserAgentString;
			request.Accept = c_ACCEPT_REQUEST;

			if (_config.HttpRequestMaxAutoRedirects > 0)
				request.MaximumAutomaticRedirections = _config.HttpRequestMaxAutoRedirects;

			if (_config.IsHttpRequestAutomaticDecompressionEnabled)
				request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			if (_config.HttpRequestTimeoutInSeconds > 0)
				request.Timeout = _config.HttpRequestTimeoutInSeconds * 1000;

			if (_config.IsSendingCookiesEnabled)
				request.CookieContainer = _cookieContainer;

			//Supposedly this does not work... https://github.com/sjdirect/abot/issues/122
			//if (_config.IsAlwaysLogin)
			//{
			//    request.Credentials = new NetworkCredential(_config.LoginUser, _config.LoginPassword);
			//    request.UseDefaultCredentials = false;
			//}
			if (_config.IsAlwaysLogin)
			{
				string credentials = Convert.ToBase64String(
					System.Text.Encoding.ASCII.GetBytes(_config.LoginUser + ":" + _config.LoginPassword)
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
			if (response != null && _config.IsSendingCookiesEnabled)
			{
				CookieCollection cookies = response.Cookies;
				_cookieContainer.Add(cookies);
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
