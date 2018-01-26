using Abot.Poco;

namespace Abot.Core.Decisions
{
	/// <summary>
	/// Determines what pages should be crawled, whether the raw content
	/// should be downloaded and if the links on a page should be crawled
	/// </summary>
	public interface ICrawlDecisionMaker
	{
		/// <summary>
		/// Decides whether the page should be crawled
		/// </summary>
		/// <param name="pageToCrawl">Page for crawling</param>
		/// <param name="crawlContext">Collect all settings for crawl</param>
		/// <returns>Decision that should crawl or not</returns>
		CrawlDecision ShouldCrawlPage(PageToCrawl pageToCrawl, CrawlContext crawlContext);

		/// <summary>
		/// Decides whether the page's links should be crawled
		/// </summary>
		/// <param name="crawledPage">Page for crawling</param>
		/// <param name="crawlContext">Collect all settings for crawl</param>
		/// <returns>Decision that should crawl or not</returns>
		CrawlDecision ShouldCrawlPageLinks(CrawledPage crawledPage, CrawlContext crawlContext);

		/// <summary>
		/// Decides whether the page's content should be dowloaded
		/// </summary>
		/// <param name="crawledPage">Page for crawling</param>
		/// <param name="crawlContext">Collect all settings for crawl</param>
		/// <returns>Decision that should crawl or not</returns>
		CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage, CrawlContext crawlContext);

		/// <summary>
		/// Decides whether the page should be re-crawled
		/// </summary>
		/// <param name="crawledPage">Page for crawling</param>
		/// <param name="crawlContext">Collect all settings for crawl</param>
		/// <returns>Decision that should crawl or not</returns>
		CrawlDecision ShouldRecrawlPage(CrawledPage crawledPage, CrawlContext crawlContext);
	}
}
