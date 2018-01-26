using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Abot.Poco;
using Abot.Utils;
using log4net;

namespace Abot.Core.Limiters
{
	/// <summary>
	/// Rate limits or throttles on a per domain basis
	/// </summary>
	[Serializable]
	public class DomainRateLimiter : IDomainRateLimiter
	{
		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		/// <summary>
		/// Rate limiter lookup
		/// </summary>
		protected ConcurrentDictionary<string, IRateLimiter> RateLimiterLookup = new ConcurrentDictionary<string, IRateLimiter>();

		/// <summary>
		/// Default min crawl delay in millisecs
		/// </summary>
		protected long DefaultMinCrawlDelayInMillisecs;

		#endregion

		#region Ctor

		/// <summary>
		/// Set <see cref="DefaultMinCrawlDelayInMillisecs"/>
		/// </summary>
		/// <param name="minCrawlDelayMillisecs"></param>
		public DomainRateLimiter(long minCrawlDelayMillisecs)
		{
			if (minCrawlDelayMillisecs < 0)
				throw new ArgumentException(nameof(minCrawlDelayMillisecs));

			// IRateLimiter is always a little under so adding a little more time
			if (minCrawlDelayMillisecs > 0)
				DefaultMinCrawlDelayInMillisecs = minCrawlDelayMillisecs + 20;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// If the domain of the param has been flagged for rate limiting,
		/// it will be rate limited according to the configured minimum crawl delay
		/// </summary>
		public void RateLimit(Uri uri)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));

			IRateLimiter rateLimiter = GetRateLimiter(uri, DefaultMinCrawlDelayInMillisecs);
			if (rateLimiter == null)
				return;

			Stopwatch timer = Stopwatch.StartNew();
			rateLimiter.WaitToProceed();
			timer.Stop();

			if (timer.ElapsedMilliseconds > 10)
				Logger.DebugFormat("Rate limited [{0}] [{1}] milliseconds", uri.AbsoluteUri, timer.ElapsedMilliseconds);
		}

		/// <summary>
		/// Add a domain entry so that domain may be rate limited according
		/// the param minumum crawl delay
		/// </summary>
		public void AddDomain(Uri uri, long minCrawlDelayInMillisecs)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));

			if (minCrawlDelayInMillisecs < 1)
				throw new ArgumentException($"{nameof(minCrawlDelayInMillisecs)} can't be < 1");

			// just calling this method adds the new domain
			GetRateLimiter(uri, Math.Max(minCrawlDelayInMillisecs, DefaultMinCrawlDelayInMillisecs));
		}

		/// <summary>
		/// Add/Update a domain entry so that domain may be rate limited according
		/// the param minumum crawl delay
		/// </summary>
		public void AddOrUpdateDomain(Uri uri, long minCrawlDelayInMillisecs)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));

			if (minCrawlDelayInMillisecs < 1)
				throw new ArgumentException(nameof(minCrawlDelayInMillisecs));

			var delayToUse = Math.Max(minCrawlDelayInMillisecs, DefaultMinCrawlDelayInMillisecs);
			if (delayToUse > 0)
			{
				var rateLimiter = new RateLimiter(1, TimeSpan.FromMilliseconds(delayToUse));

				RateLimiterLookup.AddOrUpdate(uri.Authority, rateLimiter, (key, oldValue) => rateLimiter);
				Logger.DebugFormat("Added/updated domain [{0}] with minCrawlDelayInMillisecs of [{1}] milliseconds", uri.Authority, delayToUse);
			}
		}

		/// <summary>
		/// Remove a domain entry so that it will no longer be rate limited
		/// </summary>
		public void RemoveDomain(Uri uri)
		{
			RateLimiterLookup.TryRemove(uri.Authority, out IRateLimiter rateLimiter);
		}

		#endregion

		#region Private Method

		private IRateLimiter GetRateLimiter(Uri uri, long minCrawlDelayInMillisecs)
		{
			RateLimiterLookup.TryGetValue(uri.Authority, out IRateLimiter rateLimiter);

			if (rateLimiter == null && minCrawlDelayInMillisecs > 0)
			{
				rateLimiter = new RateLimiter(1, TimeSpan.FromMilliseconds(minCrawlDelayInMillisecs));

				if (RateLimiterLookup.TryAdd(uri.Authority, rateLimiter))
					Logger.DebugFormat("Added new domain [{0}] with minCrawlDelayInMillisecs of [{1}] milliseconds", uri.Authority, minCrawlDelayInMillisecs);
				else
					Logger.WarnFormat("Unable to add new domain [{0}] with minCrawlDelayInMillisecs of [{1}] milliseconds", uri.Authority, minCrawlDelayInMillisecs);
			}

			return rateLimiter;
		}

		#endregion
	}
}
