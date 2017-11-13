using System;
using System.Collections.Generic;
using System.Linq;
using Abot.Poco;
using CsQuery;

namespace Abot.Core
{
	/// <summary>
	/// Parser that uses CsQuery https://github.com/jamietre/CsQuery to parse page links
	/// </summary>
	[Serializable]
	[Obsolete("CSQuery is no longer actively maintained. Use AngleSharpHyperlinkParser for similar usage/functionality")]
	public class CSQueryHyperlinkParser : HyperLinkParser
	{
		#region Consts
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		public const string c_HREF = "href";
		public const string c_LINK = "link";
		public const string c_A_and_AREA = "a, area";
		public const string c_BASE = "base";
		public const string c_META_NAME = "meta[name]";
		public const string c_ROBOTS = "robots";
		public const string c_CONTENT = "content";
		public const string c_REL = "rel";
		public const string c_CANONICAL = "canonical";

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		#endregion

		#region Protected Field

		/// <summary>
		/// Requare for logger information. Parser name can be equal as name of your derived class
		/// </summary>
		protected override string ParserType { get { return "CsQuery"; } }

		#endregion

		#region Ctors

		/// <summary>
		/// Create with empty configuration
		/// </summary>
		public CSQueryHyperlinkParser()
			: base()
		{ }

		/// <summary>
		/// Create Crawl Configuration by input params
		/// </summary>
		/// <param name="isRespectMetaRobotsNoFollowEnabled">Whether parser should ignore pages with meta no robots</param>
		/// <param name="isRespectAnchorRelNoFollowEnabled">Whether parser should ignore links with rel no follow</param>
		/// <param name="cleanURLFunc">Function to clean the url</param>
		/// <param name="isRespectUrlNamedAnchorOrHashbangEnabled">Whether parser should consider named anchor and/or hashbang '#' character as part of the url</param>
		[Obsolete("Use the constructor that accepts a configuration object instead")]
		public CSQueryHyperlinkParser(bool isRespectMetaRobotsNoFollowEnabled,
									  bool isRespectAnchorRelNoFollowEnabled,
									  Func<string, string> cleanURLFunc = null,
									  bool isRespectUrlNamedAnchorOrHashbangEnabled = false)
			: this(new CrawlConfiguration
			{
				IsRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled,
				IsRespectUrlNamedAnchorOrHashbangEnabled = isRespectUrlNamedAnchorOrHashbangEnabled,
				IsRespectAnchorRelNoFollowEnabled = isRespectAnchorRelNoFollowEnabled
			}, cleanURLFunc)
		{ }

		/// <summary>
		/// Create Crawl Configuration by input params
		/// </summary>
		/// <param name="config">CrawlConfiguration</param>
		/// <param name="cleanURLFunc"></param>
		public CSQueryHyperlinkParser(CrawlConfiguration config, Func<string, string> cleanURLFunc)
			: base(config, cleanURLFunc)
		{ }

		#endregion

		#region Protected Override Methods

		/// <summary>
		/// Get href values using AngleSharpHtmlDocument
		/// </summary>
		/// <param name="crawledPage">Page for parsing href values</param>
		/// <returns>Href values</returns>
		protected override IEnumerable<string> GetHrefValues(CrawledPage crawledPage)
		{
			// Don't crawl the page, if "nofollow" was found. For activating this feature check configuration
			// IsRespectHttpXRobotsTagHeaderNoFollowEnabled and IsRespectMetaRobotsNoFollowEnabled
			if (HasRobotsNoFollow(crawledPage))
				return null;

			IEnumerable<string> hrefValues = crawledPage.CsQueryDocument
				.Select(c_A_and_AREA)
				.Elements
				.Where(e => !HasRelNoFollow(e))
				.Select(y => y.GetAttribute(c_HREF))
				.Where(a => !string.IsNullOrWhiteSpace(a));

			IEnumerable<string> canonicalHref = crawledPage.CsQueryDocument
				.Select(c_LINK)
				.Elements
				.Where(e => HasRelCanonicalPointingToDifferentUrl(e, crawledPage.Uri.ToString()))
				.Select(e => e.Attributes[c_HREF]);

			return hrefValues.Concat(canonicalHref);
		}

		/// <summary>
		/// Get base url name
		/// </summary>
		/// <param name="crawledPage">Page for parsing</param>
		/// <returns>base url</returns>
		protected override string GetBaseHrefValue(CrawledPage crawledPage)
		{
			string baseTagValue = crawledPage.CsQueryDocument.Select(c_BASE).Attr(c_HREF) ?? string.Empty;
			return baseTagValue.Trim();
		}

		/// <summary>
		/// Get metadata content for robots value
		/// </summary>
		/// <param name="crawledPage">Page for parsing</param>
		/// <returns>Content for robots value</returns>
		protected override string GetMetaRobotsValue(CrawledPage crawledPage)
		{
			return crawledPage.CsQueryDocument[c_META_NAME]
				.Filter(d => d.Name.ToLowerInvariant() == c_ROBOTS)
				.Attr(c_CONTENT);
		}

		#endregion

		#region Protected Virtual Methods

		/// <summary>
		/// True, if element has "rel" attribute == "nofollow"
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		protected virtual bool HasRelNoFollow(IDomElement element)
		{
			return _config.IsRespectAnchorRelNoFollowEnabled &&
				   (element.HasAttribute(c_REL) &&
					element.GetAttribute(c_REL).ToLower().Trim() == c_NOFOLLOW);
		}

		/// <summary>
		/// Has "rel" canonical pointing To different url
		/// </summary>
		/// <param name="element"></param>
		/// <param name="orginalUrl"></param>
		/// <returns></returns>
		protected virtual bool HasRelCanonicalPointingToDifferentUrl(IDomElement element, string orginalUrl)
		{
			return element.HasAttribute(c_REL) &&
				   !string.IsNullOrWhiteSpace(element.Attributes[c_REL]) &&
				   string.Equals(element.Attributes[c_REL], c_CANONICAL, StringComparison.OrdinalIgnoreCase) &&
				   element.HasAttribute(c_HREF) &&
				   !string.IsNullOrWhiteSpace(element.Attributes[c_HREF]) &&
				   !string.Equals(element.Attributes[c_HREF], orginalUrl, StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}
}
