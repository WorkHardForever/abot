using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abot.Core;
using Abot.Core.Repositories;
using Abot.Core.Robots;
using Abot.Core.Sitemap;
using Abot.Poco;
using Abot.Util;
using CefSharp;
using CefSharp.OffScreen;
using Louw.SitemapParser;
using Robots;
using static Abot.Poco.CrawlConfiguration;

namespace Abot.Crawler
{
	/// <summary>
	/// Emulate google crawl process:
	/// 1) Try to find robots.txt. If successful, so parse sitemap-s
	/// 2) Robots.txt not found, so crawl site by base uri
	/// </summary>
	public class GoogleWebCrawler : PoliteWebCrawler, IGoogleWebCrawler
	{
		#region Protected Fields

		/// <summary>
		/// Collect sitemap from .../robots.txt
		/// </summary>
		protected IRobotsSitemap RootSitemap;

		/// <summary>
		/// Using as loading object for sitemaps
		/// </summary>
		protected IRobotsSitemapLoader SitemapLoader;

		#endregion

		#region Ctors

		/// <summary>
		/// Creates a crawler instance with custom settings or implementation
		/// </summary>
		/// <param name="threadManager">Distributes http requests over multiple threads</param>
		/// <param name="scheduler">Decides what link should be crawled next</param>
		/// <param name="pageRequester">Makes the raw http requests</param>
		/// <param name="hyperLinkParser">Parses a crawled page for it's hyperlinks</param>
		/// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
		/// <param name="crawlConfiguration">Configurable crawl values</param>
		/// <param name="memoryManager">Checks the memory usage of the host process</param>
		/// <param name="domainRateLimiter"></param>
		/// <param name="robotsDotTextFinder"></param>
		/// <param name="sitemapLoader"></param>
		public GoogleWebCrawler(
			CrawlConfiguration crawlConfiguration = null,
			ICrawlDecisionMaker crawlDecisionMaker = null,
			IThreadManager threadManager = null,
			IScheduler scheduler = null,
			IPageRequester pageRequester = null,
			IHyperLinkParser hyperLinkParser = null,
			IMemoryManager memoryManager = null,
			IDomainRateLimiter domainRateLimiter = null,
			IRobotsDotTextFinder robotsDotTextFinder = null,
			IRobotsSitemapLoader sitemapLoader = null)
			: base(crawlConfiguration, crawlDecisionMaker, threadManager, scheduler, pageRequester, hyperLinkParser, memoryManager, domainRateLimiter, robotsDotTextFinder)
		{
			SitemapLoader = sitemapLoader ?? new RobotsSitemapLoader();
		}

		#endregion

		#region Public Override Method

		/// <summary>
		/// Begins a synchronous crawl using the uri param,
		/// subscribe to events to process data as it becomes available
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="cancellationTokenSource"></param>
		/// <returns></returns>
		public override CrawlResult Crawl(Uri uri, CancellationTokenSource cancellationTokenSource)
		{
			CrawlResult result;

			// Try find robots.txt
			if (TryLoadRobotsTxt(uri) && TryParseRobotsSitemaps())
			{
				Logger.DebugFormat("Parse sitemaps uri-s:");

				// Clear depth
				var configDepth = CrawlContext.CrawlConfiguration.MaxCrawlDepth;
				CrawlContext.CrawlConfiguration.MaxCrawlDepth = 0;

				// Run robots.txt crawling
				GetSitemapResults(RootSitemap, cancellationTokenSource);

				// Restore user config
				CrawlContext.CrawlConfiguration.MaxCrawlDepth = configDepth;
			}

			// Without robots.txt we can just crawl site
			Logger.InfoFormat("*** Crawl site through getting Uri ***");
			result = base.Crawl(uri, cancellationTokenSource);

			return result;
		}

		protected virtual /*async Task<*/IEnumerable<CrawlResult>/*>*/ GetSitemapResults(IRobotsSitemap sitemap, CancellationTokenSource cancellationTokenSource)
		{
			List<CrawlResult> results = new List<CrawlResult>();

			if (!sitemap.IsLoaded)
				sitemap = SitemapLoader.Load(sitemap);

			if (sitemap.Sitemaps != null && sitemap.Sitemaps.Any())
			{
				Logger.InfoFormat("Sitemap: {0} | Inner sitemaps' count: {1}", sitemap.Location, sitemap.Sitemaps.Count());

				foreach (IRobotsSitemap derivedSitemap in sitemap.Sitemaps)
				{
					results.AddRange(/*await*/ GetSitemapResults(derivedSitemap, cancellationTokenSource)/*.Result*/);
				}
			}

			if (sitemap.Items != null && sitemap.Items.Any())
			{
				Logger.InfoFormat("Sitemap: {0} | Uris' count: {1}", sitemap.Location , sitemap.Items.Count());

			    CrawlContext.Scheduler.Add(sitemap.Items.Select(x => new PageToCrawl(x.Location)));

				CrawlResult crawlResult = new CrawlResult();
				CrawlComplete = false;
				//await Task.Run(() => CrawlSite(crawlResult));
				CrawlSite(crawlResult);
				results.Add(crawlResult);
			}

			return results;
		}

        #endregion

        #region Protected Methods

        protected override void PrintConfigValues(Uri uri)
        {
            // Print config if depth > 0. This use if we crawl site to depth, not sitemap
            if (IsPayAttention(CrawlContext.CrawlConfiguration.MaxCrawlDepth))
            {
                Logger.InfoFormat("About to crawl site [{0}]", uri.AbsoluteUri);
                base.PrintConfigValues(uri);
            }
        }

        /// <summary>
        /// Parse robots.txt sitemaps from _robotsDotText to _rootSitemap
        /// </summary>
        /// <returns></returns>
        protected virtual bool TryParseRobotsSitemaps()
		{
			IList<string> sitemaps = RobotsDotText?.Robots.GetSitemapUrls();

			// Robots.txt can collect more then 1 sitemap?
			if (sitemaps?.Count > 0)
			{
				Logger.DebugFormat("Start parse site using sitemap.xml...");

				// Collect info from robots.txt

				if (!Uri.TryCreate(sitemaps[0], UriKind.Absolute, out Uri result))
				{
					Logger.WarnFormat("Can't parse {0} to Uri object", sitemaps[0]);
					return false;
				}

				// Get root sitemap
				RootSitemap = new RobotsSitemap(
					sitemaps: sitemaps
						.Select(x => Uri.TryCreate(x, UriKind.Absolute, out Uri sitemapUri) ?
							new Sitemap(sitemapUri) : null)
						.Where(x => x != null),
					sitemapLocation: new Uri(RobotsDotText.Robots.BaseUri, Core.Robots.RobotsDotTextFinder.RobotsTxt));
			}

			return RootSitemap?.Sitemaps != null &&
				   RootSitemap.Sitemaps.Any();
		}

		private static ChromiumWebBrowser _browser;

		public static void Main(string[] args)
		{
			const string testUrl = "https://www.google.com/";

			Console.WriteLine("This example application will load {0}, take a screenshot, and save it to your desktop.", testUrl);
			Console.WriteLine("You may see Chromium debugging output, please wait...");
			Console.WriteLine();

			var settings = new CefSettings()
			{
				//By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
				CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
			};

			//Perform dependency check to make sure all relevant resources are in our output directory.
			Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

			// Create the offscreen Chromium browser.
			_browser = new ChromiumWebBrowser(testUrl);

			// An event that is fired when the first page is finished loading.
			// This returns to us from another thread.
			_browser.LoadingStateChanged += BrowserLoadingStateChanged;

			// We have to wait for something, otherwise the process will exit too soon.
			Console.ReadKey();

			// Clean up Chromium objects.  You need to call this in your application otherwise
			// you will get a crash when closing.
			Cef.Shutdown();
		}

		private static void BrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
		{
			// Check to see if loading is complete - this event is called twice, one when loading starts
			// second time when it's finished
			// (rather than an iframe within the main frame).
			if (!e.IsLoading)
			{
				// Remove the load event handler, because we only want one snapshot of the initial page.
				_browser.LoadingStateChanged -= BrowserLoadingStateChanged;

				//var scriptTask = browser.EvaluateScriptAsync("document.getElementById('lst-ib').value = 'CefSharp Was Here!'");

				//scriptTask.ContinueWith(t =>
				//{
				//	//Give the browser a little time to render
				//	Thread.Sleep(500);
				//	// Wait for the screenshot to be taken.
				//	//var task = browser.ScreenshotAsync();
				//	//task.ContinueWith(x =>
				//	//{
				//	//	// Make a file to save it to (e.g. C:\Users\jan\Desktop\CefSharp screenshot.png)
				//	//	var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

				//	//	Console.WriteLine();
				//	//	Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

				//	//	// Save the Bitmap to the path.
				//	//	// The image type is auto-detected via the ".png" extension.
				//	//	task.Result.Save(screenshotPath);

				//	//	// We no longer need the Bitmap.
				//	//	// Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
				//	//	task.Result.Dispose();

				//	//	Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

				//	//	// Tell Windows to launch the saved image.
				//	//	Process.Start(screenshotPath);

				//	//	Console.WriteLine("Image viewer launched.  Press any key to exit.");
				//	//}, TaskScheduler.Default);
				//});
			}
		}

		#endregion
	}
}
