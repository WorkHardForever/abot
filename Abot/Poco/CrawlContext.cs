﻿using System;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Threading;
using Abot.Core;
using Abot.Core.Repositories;

namespace Abot.Poco
{
	/// <summary>
	/// Context of WebCrawler. Contains all functionality of crawling
	/// </summary>
	[Serializable]
	public class CrawlContext : IDisposable
	{
		#region Ctor

		/// <summary>
		/// Configure base functionality
		/// </summary>
		public CrawlContext()
		{
			CrawlCountByDomain = new ConcurrentDictionary<string, int>();
			CancellationTokenSource = new CancellationTokenSource();
		}

		#endregion

		#region Public Variables

		/// <summary>
		/// The root of the crawl specified in the configuration.
		/// If the root URI was redirected to another URI, it will be set in RootUri.
		/// </summary>
		public Uri RootUri { get; set; }

		/// <summary>
		/// The root of the crawl
		/// </summary>
		public Uri OriginalRootUri { get; set; }

		/// <summary>
		/// total number of pages that have been crawled
		/// </summary>
		public int CrawledCount = 0;

		/// <summary>
		/// The datetime of the last unsuccessful http status (non 200) was requested
		/// </summary>
		public DateTime CrawlStartDate { get; set; }

		/// <summary>
		/// Threadsafe dictionary of domains and how many pages were crawled in that domain
		/// </summary>
		public ConcurrentDictionary<string, int> CrawlCountByDomain { get; set; }

		/// <summary>
		/// Configuration values used to determine crawl settings
		/// </summary>
		public CrawlConfiguration CrawlConfiguration { get; set; }

		/// <summary>
		/// The scheduler that is being used
		/// </summary>
		public IScheduler Scheduler { get; set; }

		/// <summary>
		/// Whether a request to stop the crawl has happened.
		/// Will clear all scheduled pages but will allow any threads that are currently crawling to complete.
		/// </summary>
		public bool IsCrawlStopRequested { get; set; }

		/// <summary>
		/// Whether a request to hard stop the crawl has happened.
		/// Will clear all scheduled pages and cancel any threads that are currently crawling.
		/// </summary>
		public bool IsCrawlHardStopRequested { get; set; }

		/// <summary>
		/// The memory usage in mb at the start of the crawl
		/// </summary>
		public int MemoryUsageBeforeCrawlInMb { get; set; }

		/// <summary>
		/// The memory usage in mb at the end of the crawl
		/// </summary>
		public int MemoryUsageAfterCrawlInMb { get; set; }

		/// <summary>
		/// Cancellation token used to hard stop the crawl.
		/// Will clear all scheduled pages and abort any threads that are currently crawling.
		/// </summary>
		public CancellationTokenSource CancellationTokenSource { get; set; }

		#endregion

		/// <summary>
		/// Dispose
		/// </summary>
	    public void Dispose()
	    {
	        Scheduler?.Dispose();
	        CancellationTokenSource?.Dispose();
	    }
	}
}
