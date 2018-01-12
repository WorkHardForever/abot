using System;
using Abot.Poco;

namespace Abot.Crawler.EventArgs
{
    [Serializable]
    public class PageLinksCrawlEventDisallowedEventArgs : PageCrawlEventCompletedEventArgs
    {
        public string DisallowedReason { get; private set; }

        public PageLinksCrawlEventDisallowedEventArgs(CrawlContext crawlContext, CrawledPage crawledPage, string disallowedReason)
            : base(crawlContext, crawledPage)
        {
            if (string.IsNullOrWhiteSpace(disallowedReason))
                throw new ArgumentNullException(nameof(disallowedReason));

            DisallowedReason = disallowedReason;
        }
    }
}
