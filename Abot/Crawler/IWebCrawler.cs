using System;
using System.Threading;
using Abot.Poco;

namespace Abot.Crawler
{
	/// <summary>
	/// Contract for base crawler of the library
	/// </summary>
	public interface IWebCrawler : IDisposable
	{
		/// <summary>
		/// Synchronous event that is fired before a page is crawled.
		/// </summary>
		event EventHandler<PageCrawlStartingArgs> PageCrawlStarting;

		/// <summary>
		/// Synchronous event that is fired when an individual page has been crawled.
		/// </summary>
		event EventHandler<PageCrawlCompletedArgs> PageCrawlCompleted;

		/// <summary>
		/// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
		/// </summary>
		event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

		/// <summary>
		/// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
		/// </summary>
		event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

		/// <summary>
		/// Asynchronous event that is fired before a page is crawled.
		/// </summary>
		event EventHandler<PageCrawlStartingArgs> PageCrawlStartingAsync;

		/// <summary>
		/// Asynchronous event that is fired when an individual page has been crawled.
		/// </summary>
		event EventHandler<PageCrawlCompletedArgs> PageCrawlCompletedAsync;

		/// <summary>
		/// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
		/// </summary>
		event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

		/// <summary>
		/// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
		/// </summary>
		event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether a page should be crawled or not
		/// </summary>
		void ShouldCrawlPage(Func<PageToCrawl, CrawlContext, CrawlDecision> decisionMaker);

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether the page's content should be dowloaded
		/// </summary>
		/// <param name="decisionMaker"></param>
		void ShouldDownloadPageContent(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether a page's links should be crawled or not
		/// </summary>
		/// <param name="decisionMaker"></param>
		void ShouldCrawlPageLinks(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether a cerain link on a page should be scheduled to be crawled
		/// </summary>
		void ShouldScheduleLink(Func<Uri, CrawledPage, CrawlContext, bool> decisionMaker);

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether a page should be recrawled
		/// </summary>
		void ShouldRecrawlPage(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether the 1st uri param is considered an internal uri to the second uri param
		/// </summary>
		/// <param name="decisionMaker delegate"></param>
		void IsInternalUri(Func<Uri, Uri, bool> decisionMaker);

		/// <summary>
		/// Begins a crawl using the uri param
		/// </summary>
		CrawlResult Crawl(Uri uri);

		/// <summary>
		/// Begins a crawl using the uri param, and can be cancelled using the CancellationToken
		/// </summary>
		CrawlResult Crawl(Uri uri, CancellationTokenSource tokenSource);

		/// <summary>
		/// Dynamic object that can hold any value that needs to be available in the crawl context
		/// </summary>
		dynamic CrawlBag { get; set; }
	}
}
