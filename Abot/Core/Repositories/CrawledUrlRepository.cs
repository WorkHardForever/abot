using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Abot.Core
{
	/// <summary>
	/// Implementation that stores a numeric hash of the url instead of the url itself
	/// to use for lookups. This should save space when the crawled url list gets very long. 
	/// </summary>
	public class CompactCrawledUrlRepository : ICrawledUrlRepository
	{
		#region Private field

		/// <summary>
		/// Why dictionary?
		/// https://social.msdn.microsoft.com/Forums/vstudio/en-US/226c8fc0-4c6b-49d0-baf3-85c658d810eb/why-is-there-no-hashset-list-in-systemcollectionsconcurrency?forum=netfxbcl
		/// </summary>
		private ConcurrentDictionary<long, byte> _urlRepository = new ConcurrentDictionary<long, byte>();

		#endregion

		#region Public Methods

		/// <summary>
		/// True, if Uri.AbsoluteUri contains in repository
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Bool</returns>
		public bool Contains(Uri uri)
		{
			return _urlRepository.ContainsKey(ComputeNumericId(uri.AbsoluteUri));
		}

		/// <summary>
		/// True, if Uri.AbsoluteUri is new in repository
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Bool</returns>
		public bool AddIfNew(Uri uri)
		{
			return _urlRepository.TryAdd(ComputeNumericId(uri.AbsoluteUri), 0);
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public virtual void Dispose()
		{
			_urlRepository = null;
		}

		#endregion

		#region Private Methods

		private long ComputeNumericId(string uri)
		{
			byte[] md5 = ToMd5Bytes(uri);

			long numericId = 0;
			for (int i = 0; i < 8; i++)
			{
				numericId += (long)md5[i] << (i * 8);
			}

			return numericId;
		}

		private byte[] ToMd5Bytes(string value)
		{
			using (MD5 md5 = MD5.Create())
			{
				return md5.ComputeHash(Encoding.Default.GetBytes(value));
			}
		}

		#endregion
	}
}
