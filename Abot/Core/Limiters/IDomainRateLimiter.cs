using System;

namespace Abot.Core.Limiters
{
	/// <summary>
	/// Rate limits or throttles on a per domain basis
	/// </summary>
	public interface IDomainRateLimiter
	{
		/// <summary>
		/// If the domain of the param has been flagged for rate limiting,
		/// it will be rate limited according to the configured minimum crawl delay
		/// </summary>
		void RateLimit(Uri uri);

		/// <summary>
		/// Add a domain entry so that domain may be rate limited according
		/// the param minumum crawl delay
		/// </summary>
		void AddDomain(Uri uri, long minCrawlDelayInMillisecs);

		/// <summary>
		/// Add/Update a domain entry so that domain may be rate limited according
		/// the param minumum crawl delay
		/// </summary>
		void AddOrUpdateDomain(Uri uri, long minCrawlDelayInMillisecs);

		/// <summary>
		/// Remove a domain entry so that it will no longer be rate limited
		/// </summary>
		void RemoveDomain(Uri uri);
	}
}
