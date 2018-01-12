using System;
using System.Threading;
using Abot.Crawler.EventArgs;
using Abot.Poco;

namespace Abot.Crawler.Interfaces
{
	/// <summary>
	/// Contract for base crawler of the library
	/// </summary>
	public interface IWebCrawler : IDisposable
	{
		/// <summary>
		/// Synchronous event that is fired before a page is crawled.
		/// </summary>
		event EventHandler<PageCrawlEventStartingEventArgs> PageCrawlStarting;

		/// <summary>
		/// Synchronous event that is fired when an individual page has been crawled.
		/// </summary>
		event EventHandler<PageCrawlEventCompletedEventArgs> PageCrawlCompleted;

		/// <summary>
		/// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
		/// </summary>
		event EventHandler<PageCrawlEventDisallowedEventArgs> PageCrawlDisallowed;

		/// <summary>
		/// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
		/// </summary>
		event EventHandler<PageLinksCrawlEventDisallowedEventArgs> PageLinksCrawlDisallowed;

		/// <summary>
		/// Asynchronous event that is fired before a page is crawled.
		/// </summary>
		event EventHandler<PageCrawlEventStartingEventArgs> PageCrawlStartingAsync;

		/// <summary>
		/// Asynchronous event that is fired when an individual page has been crawled.
		/// </summary>
		event EventHandler<PageCrawlEventCompletedEventArgs> PageCrawlCompletedAsync;

		/// <summary>
		/// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
		/// </summary>
		event EventHandler<PageCrawlEventDisallowedEventArgs> PageCrawlDisallowedAsync;

		/// <summary>
		/// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
		/// </summary>
		event EventHandler<PageLinksCrawlEventDisallowedEventArgs> PageLinksCrawlDisallowedAsync;

	    /// <summary>
	    /// Client-side delegate, which run when crawl decide crawl or not
	    /// if all config conditions are succeeded
	    /// </summary>
	    Func<PageToCrawl, CrawlContext, CrawlDecision> ShouldCrawlPageDecisionMaker { get; set; }

	    Func<CrawledPage, CrawlContext, CrawlDecision> ShouldDownloadPageContentDecisionMaker { get; set; }

	    Func<CrawledPage, CrawlContext, CrawlDecision> ShouldCrawlPageLinksDecisionMaker { get; set; }

	    Func<CrawledPage, CrawlContext, CrawlDecision> ShouldRecrawlPageDecisionMaker { get; set; }

	    Func<Uri, CrawledPage, CrawlContext, bool> ShouldScheduleLinkDecisionMaker { get; set; }

	    Func<Uri, Uri, bool> IsInternalDecisionMaker { get; set; }

        /// <summary>
        /// Begins a crawl using the uri param
        /// </summary>
        CrawlResult Crawl(Uri uri);

		/// <summary>
		/// Begins a crawl using the uri param, and can be cancelled using the CancellationToken
		/// </summary>
		CrawlResult Crawl(Uri uri, CancellationTokenSource tokenSource);
	}
}
