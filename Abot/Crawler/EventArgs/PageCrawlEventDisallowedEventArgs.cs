using System;
using Abot.Poco;

namespace Abot.Crawler.EventArgs
{
    [Serializable]
    public class PageCrawlEventDisallowedEventArgs: PageCrawlEventStartingEventArgs
    {
        public string DisallowedReason { get; }

        public PageCrawlEventDisallowedEventArgs(CrawlContext crawlContext, PageToCrawl pageToCrawl, string disallowedReason)
            : base(crawlContext, pageToCrawl)
        {
            if (string.IsNullOrWhiteSpace(disallowedReason))
                throw new ArgumentNullException(nameof(disallowedReason));

            DisallowedReason = disallowedReason;
        }
    }
}
