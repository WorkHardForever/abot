using System;
using System.Threading.Tasks;
using Abot.Poco;

namespace Abot.Core.Requests
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
    }
}