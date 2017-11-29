using System;
using System.Collections.Concurrent;
using Abot.Poco;

namespace Abot.Core.Repositories
{
	/// <summary>
	/// Repository with pages' queue for next crawling
	/// </summary>
	[Serializable]
	public class QueueOfPagesToCrawlRepository : IQueueOfPagesToCrawlRepository
	{
		#region Private field

	    private ConcurrentQueue<PageToCrawl> _urlQueue = new ConcurrentQueue<PageToCrawl>();

		#endregion

		#region Public Methods

		/// <summary>
		/// Add page to the queue
		/// </summary>
		/// <param name="page"></param>
		public void Add(PageToCrawl page)
		{
			_urlQueue.Enqueue(page);
		}

		/// <summary>
		/// Cut page to crawl from the top of the queue
		/// </summary>
		/// <returns></returns>
		public PageToCrawl GetNext()
		{
			_urlQueue.TryDequeue(out PageToCrawl pageToCrawl);
			return pageToCrawl;
		}

		/// <summary>
		/// Clear queue
		/// </summary>
		public void Clear()
		{
			_urlQueue = new ConcurrentQueue<PageToCrawl>();
		}

		/// <summary>
		/// Get count of queue
		/// </summary>
		/// <returns></returns>
		public int Count()
		{
			return _urlQueue.Count;
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public virtual void Dispose()
		{
			_urlQueue = null;
		}

		#endregion
	}
}
