using System;

namespace Abot.Core
{
	/// <summary>
	/// Contract to stores crawled urls
	/// </summary>
	public interface ICrawledUrlRepository : IDisposable
	{
		/// <summary>
		/// True, if Uri contains in repository
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Bool</returns>
		bool Contains(Uri uri);

		/// <summary>
		/// True, if Uri is new in repository
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Bool</returns>
		bool AddIfNew(Uri uri);
	}
}
