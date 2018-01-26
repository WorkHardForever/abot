using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Abot.Poco;

namespace Abot.Core.Decisions
{
    /// <summary>
    /// Determines what pages should be crawled, whether the raw content
    /// should be downloaded and if the links on a page should be crawled
    /// </summary>
    [Serializable]
    public class CrawlDecisionMaker : ICrawlDecisionMaker
    {
        #region Const

        /// <summary>
        /// All uris should start with "http(s)://"
        /// </summary>
        public const string UriStartWithHttp = "http";

        #endregion

        #region Public Methods

        /// <summary>
        /// Decides whether the page should be crawled
        /// </summary>
        /// <param name="pageToCrawl">Page for crawling</param>
        /// <param name="crawlContext">Collect all settings for crawl</param>
        /// <returns>Decision that should crawl or not</returns>
        public virtual CrawlDecision ShouldCrawlPage(PageToCrawl pageToCrawl, CrawlContext crawlContext)
        {
            if (pageToCrawl == null)
                return new CrawlDecision { Allow = false, Reason = "Null page to crawl" };

            if (crawlContext == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawl context" };

            if (pageToCrawl.RedirectedFrom != null &&
                pageToCrawl.RedirectPosition > crawlContext.CrawlConfiguration.HttpRequestMaxAutoRedirects)
                return new CrawlDecision
                {
                    Allow = false,
                    Reason =
                        $"HttpRequestMaxAutoRedirects limit of " +
                        $"[{crawlContext.CrawlConfiguration.HttpRequestMaxAutoRedirects}] has been reached"
                };

            if (pageToCrawl.CrawlDepth > crawlContext.CrawlConfiguration.MaxCrawlDepth)
                return new CrawlDecision { Allow = false, Reason = "Crawl depth is above max" };

            if (!pageToCrawl.Uri.Scheme.StartsWith(UriStartWithHttp))
                return new CrawlDecision { Allow = false, Reason = "Scheme does not begin with http" };

            // TODO Do we want to ignore redirect chains (ie.. do not treat them as seperate page crawls)?
            if (!pageToCrawl.IsRetry &&
                CrawlConfiguration.IsPayAttention(crawlContext.CrawlConfiguration.MaxPagesToCrawl) &&
                crawlContext.CrawledCount + crawlContext.Scheduler.Count + 1 > crawlContext.CrawlConfiguration.MaxPagesToCrawl)
            {
                return new CrawlDecision
                {
                    Allow = false,
                    Reason =
                        $"MaxPagesToCrawl limit of [{crawlContext.CrawlConfiguration.MaxPagesToCrawl}] has been reached"
                };
            }

            if (!pageToCrawl.IsRetry &&
                CrawlConfiguration.IsPayAttention(crawlContext.CrawlConfiguration.MaxPagesToCrawlPerDomain) &&
                crawlContext.CrawlCountByDomain.TryGetValue(pageToCrawl.Uri.Authority, out int pagesCrawledInThisDomain) &&
                CrawlConfiguration.IsPayAttention(pagesCrawledInThisDomain) &&
                pagesCrawledInThisDomain >= crawlContext.CrawlConfiguration.MaxPagesToCrawlPerDomain)
                return new CrawlDecision
                {
                    Allow = false,
                    Reason =
                        $"MaxPagesToCrawlPerDomain limit of [{crawlContext.CrawlConfiguration.MaxPagesToCrawlPerDomain}] " +
                        $"has been reached for domain [{pageToCrawl.Uri.Authority}]"
                };

            if (!pageToCrawl.IsInternal && !crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled)
                return new CrawlDecision { Allow = false, Reason = "Link is external" };

            return new CrawlDecision { Allow = true };
        }

        /// <summary>
        /// Decides whether the page's links should be crawled
        /// </summary>
        /// <param name="crawledPage">Page for crawling</param>
        /// <param name="crawlContext">Collect all settings for crawl</param>
        /// <returns>Decision that should crawl or not</returns>
        public virtual CrawlDecision ShouldCrawlPageLinks(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            if (crawledPage == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawled page" };

            if (crawlContext == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawl context" };

            if (string.IsNullOrWhiteSpace(crawledPage.Content.Text))
                return new CrawlDecision { Allow = false, Reason = "Page has no content" };

            if (!crawlContext.CrawlConfiguration.IsExternalPageLinksCrawlingEnabled &&
                !crawledPage.IsInternal)
                return new CrawlDecision { Allow = false, Reason = "Link is external" };

            if (crawledPage.CrawlDepth >= crawlContext.CrawlConfiguration.MaxCrawlDepth)
                return new CrawlDecision { Allow = false, Reason = "Crawl depth is above max" };

            return new CrawlDecision { Allow = true };
        }

        /// <summary>
        /// Decides whether the page's content should be dowloaded
        /// </summary>
        /// <param name="crawledPage">Page for crawling</param>
        /// <param name="crawlContext">Collect all settings for crawl</param>
        /// <returns>Decision that should crawl or not</returns>
        public virtual CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            if (crawledPage == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawled page" };

            if (crawlContext == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawl context" };

            if (crawledPage.HttpWebResponse == null)
                return new CrawlDecision { Allow = false, Reason = "Null HttpWebResponse" };

            if (crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                return new CrawlDecision { Allow = false, Reason = $"Status code {crawledPage.HttpWebResponse.StatusCode}" };

            if (!IsDownloadableByContentType(crawledPage, crawlContext, out List<string> cleanDownloadableContentTypes))
                return new CrawlDecision { Allow = false, Reason = "Content type is not any of the following: " + string.Join(",", cleanDownloadableContentTypes) };

            if (CrawlConfiguration.IsPayAttention(crawlContext.CrawlConfiguration.MaxPageSizeInBytes) &&
                crawledPage.HttpWebResponse.ContentLength > crawlContext.CrawlConfiguration.MaxPageSizeInBytes)
                return new CrawlDecision
                {
                    Allow = false,
                    Reason =
                        $"Page size of [{crawledPage.HttpWebResponse.ContentLength}] bytes is above the max allowable of " +
                        $"[{crawlContext.CrawlConfiguration.MaxPageSizeInBytes}] bytes"
                };

            return new CrawlDecision { Allow = true };
        }

        /// <summary>
        /// Decides whether the page should be re-crawled
        /// </summary>
        /// <param name="crawledPage">Page for crawling</param>
        /// <param name="crawlContext">Collect all settings for crawl</param>
        /// <returns>Decision that should crawl or not</returns>
        public virtual CrawlDecision ShouldRecrawlPage(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            if (crawledPage == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawled page" };

            if (crawlContext == null)
                return new CrawlDecision { Allow = false, Reason = "Null crawl context" };

            if (crawledPage.WebException == null)
                return new CrawlDecision { Allow = false, Reason = "WebException did not occur" };

            if (CrawlConfiguration.IsPayAttention(crawlContext.CrawlConfiguration.MaxRetryCount))
                return new CrawlDecision { Allow = false, Reason = "MaxRetryCount is less than 1" };

            if (crawledPage.RetryCount >= crawlContext.CrawlConfiguration.MaxRetryCount)
                return new CrawlDecision { Allow = false, Reason = "MaxRetryCount has been reached" };

            return new CrawlDecision { Allow = true };
        }

        #endregion

        #region Protected Method

        /// <summary>
        /// Equal config content type with crawl page content type
        /// </summary>
        /// <param name="crawledPage">Page for crawling</param>
        /// <param name="crawlContext">Collect all settings for crawl</param>
        /// <param name="cleanDownloadableContentTypes">Available content types from the page</param>
        /// <returns>Decision that should crawl or not</returns>
        protected virtual bool IsDownloadableByContentType(CrawledPage crawledPage, CrawlContext crawlContext, out List<string> cleanDownloadableContentTypes)
        {
            string pageContentType = crawledPage.HttpWebResponse.ContentType.ToLower().Trim();
            cleanDownloadableContentTypes = crawlContext.CrawlConfiguration.DownloadableContentTypes
                .Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            return cleanDownloadableContentTypes.Any(
                downloadableContentType => pageContentType.Contains(downloadableContentType.ToLower().Trim())
            );
        }

        #endregion
    }
}
