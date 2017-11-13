using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Abot.Poco;
using log4net;

namespace Abot.Core
{
	/// <summary>
	/// Handles parsing hyperlinks out of the raw html
	/// </summary>
	[Serializable]
	public abstract class HyperLinkParser : IHyperLinkParser
	{
		#region Consts
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		public const string c_NOFOLLOW = "nofollow";
		public const string c_NONE = "none";
		public const string c_X_ROBOT_TAG = "X-Robots-Tag";

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		#endregion

		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog _logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		/// <summary>
		/// Crawl config
		/// </summary>
		protected CrawlConfiguration _config;

		/// <summary>
		/// Requare Uri.AbsoluteUri as param.
		/// Return modify url
		/// </summary>
		protected Func<string, string> _cleanURLFunc;

		#endregion

		#region Protected Abstract Field

		/// <summary>
		/// Requare for logger information. Parser name can be equal as name of your derived class
		/// </summary>
		protected abstract string ParserType { get; }

		#endregion

		#region Ctors

		/// <summary>
		/// Constructor that accepts an empty configuration object
		/// NOTE: Use as : base() in your derived classes
		/// </summary>
		protected HyperLinkParser()
			: this(new CrawlConfiguration(), null)
		{ }

		/// <summary>
		/// Constructor that accepts a configuration object instead
		/// NOTE: Use as : base(...) in your derived classes
		/// </summary>
		[Obsolete("Use the constructor that accepts a configuration object instead")]
		protected HyperLinkParser(bool isRespectMetaRobotsNoFollowEnabled,
								  bool isRespectUrlNamedAnchorOrHashbangEnabled,
								  Func<string, string> cleanURLFunc)
			: this(new CrawlConfiguration
			{
				IsRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled,
				IsRespectUrlNamedAnchorOrHashbangEnabled = isRespectUrlNamedAnchorOrHashbangEnabled
			}, cleanURLFunc)
		{ }

		/// <summary>
		/// Constructor that accepts a configuration object
		/// NOTE: Use as : base(...) in your derived classes
		/// </summary>
		protected HyperLinkParser(CrawlConfiguration config, Func<string, string> cleanURLFunc)
		{
			_config = config;
			_cleanURLFunc = cleanURLFunc;
		}

		#endregion

		#region Public Method

		/// <summary>
		/// Parses html to extract hyperlinks, converts each into an absolute url
		/// </summary>
		public virtual IEnumerable<Uri> GetLinks(CrawledPage crawledPage)
		{
			CheckParams(crawledPage);

			Stopwatch timer = Stopwatch.StartNew();

			List<Uri> uris = GetUris(crawledPage, GetHrefValues(crawledPage));

			timer.Stop();
			_logger.DebugFormat("{0} parsed links from [{1}] in [{2}] milliseconds", ParserType, crawledPage.Uri, timer.ElapsedMilliseconds);

			return uris;
		}

		#endregion

		#region Protected Abstract Methods

		/// <summary>
		/// Get href values
		/// </summary>
		/// <param name="crawledPage">Page for parsing href values</param>
		/// <returns>Href values</returns>
		protected abstract IEnumerable<string> GetHrefValues(CrawledPage crawledPage);

		/// <summary>
		/// Get base url name
		/// </summary>
		/// <param name="crawledPage">Page for parsing</param>
		/// <returns>base url</returns>
		protected abstract string GetBaseHrefValue(CrawledPage crawledPage);

		/// <summary>
		/// Get metadata content for robots value
		/// </summary>
		/// <param name="crawledPage">Page for parsing</param>
		/// <returns>Content for robots value</returns>
		protected abstract string GetMetaRobotsValue(CrawledPage crawledPage);

		#endregion

		#region Protected Methods

		/// <summary>
		/// Check page for validation
		/// </summary>
		/// <param name="crawledPage">Page for validation</param>
		protected virtual void CheckParams(CrawledPage crawledPage)
		{
			if (crawledPage == null)
				throw new ArgumentNullException("crawledPage");
		}

		/// <summary>
		/// Get uris from href values from crawled page
		/// </summary>
		/// <param name="crawledPage">Current crawled page</param>
		/// <param name="hrefValues">href values</param>
		/// <returns>list of uris</returns>
		protected virtual List<Uri> GetUris(CrawledPage crawledPage, IEnumerable<string> hrefValues)
		{
			List<Uri> uris = new List<Uri>();
			if (hrefValues == null || hrefValues.Count() < 1)
				return uris;

			// Use the uri of the page that actually responded to the request instead of crawledPage.Uri (Issue 82).
			// Using HttpWebRequest.Address instead of HttpWebResonse.ResponseUri since this is the best practice and mentioned
			// on http://msdn.microsoft.com/en-us/library/system.net.httpwebresponse.responseuri.aspx
			Uri uriToUse = crawledPage.HttpWebRequest.Address ?? crawledPage.Uri;

			//If html base tag exists use it instead of page uri for relative links
			string baseHref = GetBaseHrefValue(crawledPage);

			if (!string.IsNullOrEmpty(baseHref))
			{
				if (baseHref.StartsWith("//"))
					baseHref = crawledPage.Uri.Scheme + ":" + baseHref;

				try
				{
					uriToUse = new Uri(baseHref);
				}
				catch (Exception e)
				{
					_logger.FatalFormat("Can't convert {0} to Uri. {0}: {1}", nameof(baseHref), baseHref);
					throw e;
				}
			}

			foreach (string hrefValue in hrefValues)
			{
				try
				{
					// Remove the url fragment part of the url if needed.
					// This is the part after the # and is often not useful.
					string href = _config.IsRespectUrlNamedAnchorOrHashbangEnabled ?
						hrefValue :
						hrefValue.Split('#')[0];

					Uri newUri = new Uri(uriToUse, href);

					if (_cleanURLFunc != null)
						newUri = new Uri(_cleanURLFunc(newUri.AbsoluteUri));

					if (!uris.Exists(u => u.AbsoluteUri == newUri.AbsoluteUri))
						uris.Add(newUri);
				}
				catch (Exception e)
				{
					_logger.DebugFormat("Could not parse link [{0}] on page [{1}]", hrefValue, crawledPage.Uri);
					_logger.Debug(e);
				}
			}

			return uris;
		}

		/// <summary>
		/// Get true, if page contain "nofollow" or "none" in header or meta
		/// </summary>
		/// <param name="crawledPage"></param>
		/// <returns></returns>
		protected virtual bool HasRobotsNoFollow(CrawledPage crawledPage)
		{
			//X-Robots-Tag http header
			if (_config.IsRespectHttpXRobotsTagHeaderNoFollowEnabled)
			{
				var xRobotsTagHeader = crawledPage.HttpWebResponse.Headers[c_X_ROBOT_TAG];
				if (xRobotsTagHeader != null &&
					(xRobotsTagHeader.ToLower().Contains(c_NOFOLLOW) ||
					 xRobotsTagHeader.ToLower().Contains(c_NONE)))
				{
					_logger.InfoFormat("Http header X-Robots-Tag nofollow detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);
					return true;
				}
			}

			//Meta robots tag
			if (_config.IsRespectMetaRobotsNoFollowEnabled)
			{
				string robotsMeta = GetMetaRobotsValue(crawledPage);
				if (robotsMeta != null &&
					(robotsMeta.ToLower().Contains(c_NOFOLLOW) ||
					 robotsMeta.ToLower().Contains(c_NONE)))
				{
					_logger.InfoFormat("Meta Robots nofollow tag detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);
					return true;
				}
			}

			return false;
		}

		#endregion
	}
}
