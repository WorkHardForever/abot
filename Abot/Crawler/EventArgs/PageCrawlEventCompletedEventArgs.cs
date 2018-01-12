using System;
using Abot.Poco;

namespace Abot.Crawler.EventArgs
{
    [Serializable]
    public class PageCrawlEventCompletedEventArgs : CrawlEventArgs
    {
        public CrawledPage CrawledPage { get; }

        public PageCrawlEventCompletedEventArgs(CrawlContext crawlContext, CrawledPage crawledPage)
            : base(crawlContext)
        {
            if (crawledPage == null)
                throw new ArgumentNullException(nameof(crawledPage));

            CrawledPage = crawledPage;
        }
    }
}
