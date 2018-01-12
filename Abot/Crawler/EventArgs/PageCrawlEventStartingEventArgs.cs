using System;
using Abot.Poco;

namespace Abot.Crawler.EventArgs
{
    [Serializable]
    public class PageCrawlEventStartingEventArgs : CrawlEventArgs
    {
        public PageToCrawl PageToCrawl { get; }

        public PageCrawlEventStartingEventArgs(CrawlContext crawlContext, PageToCrawl pageToCrawl)
            : base(crawlContext)
        {
            if (pageToCrawl == null)
                throw new ArgumentNullException(nameof(pageToCrawl));

            PageToCrawl = pageToCrawl;
        }
    }
}
