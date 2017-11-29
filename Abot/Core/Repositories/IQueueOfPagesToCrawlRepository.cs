using System;
using Abot.Poco;

namespace Abot.Core.Repositories
{
	/// <summary>
	/// Repository with pages' queue for next crawling
	/// </summary>
	public interface IQueueOfPagesToCrawlRepository : IDisposable
	{
		/// <summary>
		/// Add page to the queue
		/// </summary>
		/// <param name="page"></param>
		void Add(PageToCrawl page);

		/// <summary>
		/// Get next page to crawl
		/// </summary>
		/// <returns></returns>
		PageToCrawl GetNext();

		/// <summary>
		/// Clear queue
		/// </summary>
		void Clear();

		/// <summary>
		/// Get count of queue
		/// </summary>
		/// <returns></returns>
		int Count();
	}
}
