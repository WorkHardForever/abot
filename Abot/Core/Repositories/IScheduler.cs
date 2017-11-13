using System;
using System.Collections.Generic;
using Abot.Poco;

namespace Abot.Core
{
	/// <summary>
	/// Handles managing the priority of what pages need to be crawled
	/// </summary>
	public interface IScheduler : IDisposable
	{
		/// <summary>
		/// Count of remaining items that are currently scheduled
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Schedules the param to be crawled
		/// </summary>
		void Add(PageToCrawl page);

		/// <summary>
		/// Schedules the param to be crawled
		/// </summary>
		void Add(IEnumerable<PageToCrawl> pages);

		/// <summary>
		/// Gets the next page to crawl
		/// </summary>
		PageToCrawl GetNext();

		/// <summary>
		/// Clear all currently scheduled pages
		/// </summary>
		void Clear();

		/// <summary>
		/// Add the Url to the list of crawled Url without scheduling it to be crawled.
		/// </summary>
		/// <param name="uri"></param>
		void AddKnownUri(Uri uri);

		/// <summary>
		/// Returns whether or not the specified Uri was already scheduled to be crawled or simply added to the
		/// list of known Uris.
		/// </summary>
		bool IsUriKnown(Uri uri);
	}
}
