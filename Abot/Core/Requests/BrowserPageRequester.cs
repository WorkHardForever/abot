using System;
using System.Threading.Tasks;
using Abot.Poco;
using CefSharp.OffScreen;
using log4net;

namespace Abot.Core.Requests
{
	/// <summary>
	/// Page requests translate due to chromium browser
	/// </summary>
	public class BrowserPageRequester : IPageRequester
	{
		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		/// <summary>
		/// Config with all options to get page or miss it
		/// </summary>
		protected CrawlConfiguration Config;

		/// <summary>
		/// Emulate browser work
		/// </summary>
		protected ChromiumWebBrowser Browser;

		#endregion

		#region Ctors

		/// <summary>
		/// Set received config
		/// </summary>
		/// <param name="config"></param>
		public BrowserPageRequester(CrawlConfiguration config)
			: this(config, null) { }

		/// <summary>
		/// Set received config
		/// </summary>
		/// <param name="config"></param>
		/// <param name="browser"></param>
		public BrowserPageRequester(CrawlConfiguration config, ChromiumWebBrowser browser)
		{
			Config = config ?? throw new ArgumentNullException(nameof(config));
			Browser = browser ?? new ChromiumWebBrowser();
		}

		#endregion

		/// <summary>
		/// Make an http web request to the url and download its content
		/// </summary>
		public Task<CrawledPage> MakeRequestAsync(Uri uri) =>
			MakeRequestAsync(uri, x => new CrawlDecision { Allow = true });

		/// <summary>
		/// Make an http web request to the url and download its content based on the param func decision
		/// </summary>
		public Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			Browser?.Dispose();
		}
	}
}
