using Abot.Poco;
using System;
using System.Threading.Tasks;

namespace Abot.Core
{
	/// <summary>
	/// Work with requesting pages and getting its contents
	/// </summary>
	public interface IPageRequester : IDisposable
    {
        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        Task<CrawledPage> MakeRequestAsync(Uri uri);

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);

        ///// <summary>
        ///// Asynchronously make an http web request to the url and download its content based on the param func decision
        ///// </summary>
        //Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);
    }
}