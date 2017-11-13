using System;

namespace Abot.Poco
{
	/// <summary>
	/// Decision, that allow to crawl next or collect
	/// reason why not. Also collect requests to stop crawls
	/// </summary>
	[Serializable]
	public class CrawlDecision
	{
		#region Ctor

		/// <summary>
		/// Configure base functionality
		/// </summary>
		public CrawlDecision()
		{
			Reason = string.Empty;
		}

		#endregion

		#region Public Variables

		/// <summary>
		/// Whether to allow the crawl decision
		/// </summary>
		public bool Allow { get; set; }

		/// <summary>
		/// The reason the crawl decision was NOT allowed
		/// </summary>
		public string Reason { get; set; }

		/// <summary>
		/// Whether the crawl should be stopped.
		/// Will clear all scheduled pages but will allow any threads that are currently crawling to complete.
		/// </summary>
		public bool ShouldStopCrawl { get; set; }

		/// <summary>
		/// Whether the crawl should be "hard stopped".
		/// Will clear all scheduled pages and cancel any threads that are currently crawling.
		/// </summary>
		public bool ShouldHardStopCrawl { get; set; }

		#endregion
	}
}
