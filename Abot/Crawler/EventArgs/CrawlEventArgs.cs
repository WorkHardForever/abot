using System;
using Abot.Poco;

namespace Abot.Crawler.EventArgs
{
    [Serializable]
    public class CrawlEventArgs : System.EventArgs
    {
        public CrawlContext CrawlContext { get; }

        public CrawlEventArgs(CrawlContext crawlContext)
        {
            if (crawlContext == null)
                throw new ArgumentNullException(nameof(crawlContext));

            CrawlContext = crawlContext;
        }
    }
}
