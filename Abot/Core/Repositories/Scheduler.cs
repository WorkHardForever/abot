using System;
using System.Collections.Generic;
using Abot.Poco;

namespace Abot.Core
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
		protected ICrawledUrlRepository _crawledUrlRepository;

		/// <summary>
		/// Pages to crawl repository
		/// </summary>
		protected IQueueOfPagesToCrawlRepository _pagesToCrawlRepository;

		/// <summary>
		/// Allow crawl this uri again if something was fail?
		/// </summary>
		protected bool _allowUriRecrawling;

		#endregion

		#region Public Field

		/// <summary>
		/// Count of remaining items that are currently scheduled
		/// </summary>
		public int Count { get { return _pagesToCrawlRepository.Count(); } }

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
			_allowUriRecrawling = allowUriRecrawling;
			_crawledUrlRepository = crawledUrlRepository ?? new CompactCrawledUrlRepository();
			_pagesToCrawlRepository = pagesToCrawlRepository ?? new QueueOfPagesToCrawlRepository();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Schedules the param to be crawled
		/// </summary>
		public void Add(PageToCrawl page)
		{
			if (page == null)
				throw new ArgumentNullException("page");

			if (_allowUriRecrawling || page.IsRetry)
			{
				_pagesToCrawlRepository.Add(page);
			}
			else
			{
				if (_crawledUrlRepository.AddIfNew(page.Uri))
					_pagesToCrawlRepository.Add(page);
			}
		}

		/// <summary>
		/// Schedules the param to be crawled
		/// </summary>
		public void Add(IEnumerable<PageToCrawl> pages)
		{
			if (pages == null)
				throw new ArgumentNullException("pages");

			foreach (PageToCrawl page in pages)
				Add(page);
		}

		/// <summary>
		/// Gets the next page to crawl
		/// </summary>
		public PageToCrawl GetNext()
		{
			return _pagesToCrawlRepository.GetNext();
		}

		/// <summary>
		/// Clear all currently scheduled pages
		/// </summary>
		public void Clear()
		{
			_pagesToCrawlRepository.Clear();
		}

		/// <summary>
		/// Add the Url to the list of crawled Url without scheduling it to be crawled.
		/// </summary>
		/// <param name="uri"></param>
		public void AddKnownUri(Uri uri)
		{
			_crawledUrlRepository.AddIfNew(uri);
		}

		/// <summary>
		/// Returns whether or not the specified Uri was already scheduled to be crawled or simply added to the
		/// list of known Uris.
		/// </summary>
		public bool IsUriKnown(Uri uri)
		{
			return _crawledUrlRepository.Contains(uri);
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			if (_crawledUrlRepository != null)
			{
				_crawledUrlRepository.Dispose();
			}
			if (_pagesToCrawlRepository != null)
			{
				_pagesToCrawlRepository.Dispose();
			}
		}

		#endregion
	}
}
