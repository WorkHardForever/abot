using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abot.Core;
using Abot.Core.Sitemap;
using Abot.Poco;
using Abot.Util;
using Louw.SitemapParser;
using Robots;

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
		protected IRobotsSitemap _rootSitemap;

		/// <summary>
		/// Using as loading object for sitemaps
		/// </summary>
		protected IRobotsSitemapLoader _sitemapLoader;

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
			_sitemapLoader = sitemapLoader ?? new RobotsSitemapLoader();
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
				var configDepth = _crawlContext.CrawlConfiguration.MaxCrawlDepth;
				_crawlContext.CrawlConfiguration.MaxCrawlDepth = 0;

				// Run robots.txt crawling
				GetSitemapResults(_rootSitemap, cancellationTokenSource);

				// Restore user config
				_crawlContext.CrawlConfiguration.MaxCrawlDepth = configDepth;
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
				sitemap = _sitemapLoader.Load(sitemap);

			if (sitemap.Sitemaps != null && sitemap.Sitemaps.Any())
			{
				Logger.InfoFormat("Sitemap: {0} | Locs' count: {1}", sitemap.Location, sitemap.Sitemaps.Count());

				foreach (IRobotsSitemap derivedSitemap in sitemap.Sitemaps)
				{
					results.AddRange(/*await*/ GetSitemapResults(derivedSitemap, cancellationTokenSource)/*.Result*/);
				}
			}

			if (sitemap.Items != null && sitemap.Items.Any())
			{
				Logger.InfoFormat("Uris' count: {0}", sitemap.Items.Count());

				_scheduler.Add(sitemap.Items.Select(x => new PageToCrawl(x.Location)));

				CrawlResult crawlResult = new CrawlResult();
				_crawlComplete = false;
				//await Task.Run(() => CrawlSite(crawlResult));
				CrawlSite(crawlResult);
				results.Add(crawlResult);
			}

			return results;
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Parse robots.txt sitemaps from _robotsDotText to _rootSitemap
		/// </summary>
		/// <returns></returns>
		protected virtual bool TryParseRobotsSitemaps()
		{
			IList<string> sitemaps = _robotsDotText?.Robots.GetSitemapUrls();

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
				_rootSitemap = new RobotsSitemap(
					sitemaps: sitemaps
						.Select(x => Uri.TryCreate(x, UriKind.Absolute, out Uri sitemapUri) ?
							new Sitemap(sitemapUri) : null)
						.Where(x => x != null),
					sitemapLocation: new Uri(_robotsDotText.Robots.BaseUri, RobotsDotTextFinder.c_ROBOTS_TXT));
			}

			return _rootSitemap?.Sitemaps != null &&
				   _rootSitemap.Sitemaps.Any();
		}

		#endregion
	}
}
