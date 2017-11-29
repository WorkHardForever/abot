using System;
using System.Collections.Generic;
using Abot.Poco;

namespace Abot.Core.Repositories
{
	/// <summary>
	/// Handles managing the priority of what pages need to be crawled
	/// </summary>
	[Serializable]
	public class Scheduler : IScheduler
	{
		#region Protected Fields

		/// <summary>
		/// Crawled url repository
		/// </summary>
		protected ICrawledUrlRepository CrawledUrlRepository;

		/// <summary>
		/// Pages to crawl repository
		/// </summary>
		protected IQueueOfPagesToCrawlRepository PagesToCrawlRepository;

		/// <summary>
		/// Allow crawl this uri again if something was fail?
		/// </summary>
		protected bool AllowUriRecrawling;

		#endregion

		#region Public Field

		/// <summary>
		/// Count of remaining items that are currently scheduled
		/// </summary>
		public int Count => PagesToCrawlRepository.Count();

		#endregion

		#region Ctors

		/// <summary>
		/// Create default repositories for scheduler
		/// NOTE: allowUriRecrawling = false
		/// </summary>
		public Scheduler()
			: this(false, null, null)
		{ }

		/// <summary>
		/// Create custom repositories for scheduler or default if these are null
		/// </summary>
		/// <param name="allowUriRecrawling">Allow crawl this uri again if something was fail?</param>
		/// <param name="crawledUrlRepository">Crawled url repository</param>
		/// <param name="pagesToCrawlRepository">Pages to crawl repository</param>
		public Scheduler(bool allowUriRecrawling,
						 ICrawledUrlRepository crawledUrlRepository,
						 IQueueOfPagesToCrawlRepository pagesToCrawlRepository)
		{
			AllowUriRecrawling = allowUriRecrawling;
			CrawledUrlRepository = crawledUrlRepository ?? new CompactCrawledUrlRepository();
			PagesToCrawlRepository = pagesToCrawlRepository ?? new QueueOfPagesToCrawlRepository();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Schedules the param to be crawled
		/// </summary>
		public void Add(PageToCrawl page)
		{
			if (page == null)
				throw new ArgumentNullException(nameof(page));

			if (AllowUriRecrawling || page.IsRetry)
			{
				PagesToCrawlRepository.Add(page);
			}
			else
			{
				if (CrawledUrlRepository.AddIfNew(page.Uri))
					PagesToCrawlRepository.Add(page);
			}
		}

		/// <summary>
		/// Schedules the param to be crawled
		/// </summary>
		public void Add(IEnumerable<PageToCrawl> pages)
		{
			if (pages == null)
				throw new ArgumentNullException(nameof(pages));

			foreach (PageToCrawl page in pages)
				Add(page);
		}

		/// <summary>
		/// Gets the next page to crawl
		/// </summary>
		public PageToCrawl GetNext()
		{
			return PagesToCrawlRepository.GetNext();
		}

		/// <summary>
		/// Clear all currently scheduled pages
		/// </summary>
		public void Clear()
		{
			PagesToCrawlRepository.Clear();
		}

		/// <summary>
		/// Add the Url to the list of crawled Url without scheduling it to be crawled.
		/// </summary>
		/// <param name="uri"></param>
		public void AddKnownUri(Uri uri)
		{
			CrawledUrlRepository.AddIfNew(uri);
		}

		/// <summary>
		/// Returns whether or not the specified Uri was already scheduled to be crawled or simply added to the
		/// list of known Uris.
		/// </summary>
		public bool IsUriKnown(Uri uri)
		{
			return CrawledUrlRepository.Contains(uri);
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
		    CrawledUrlRepository?.Dispose();
		    PagesToCrawlRepository?.Dispose();
		}

		#endregion
	}
}
