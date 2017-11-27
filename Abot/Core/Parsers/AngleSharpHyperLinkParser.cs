using System;
using System.Collections.Generic;
using System.Linq;
using Abot.Poco;
using AngleSharp.Dom;

namespace Abot.Core
{
	/// <summary>
	/// Parser that uses AngleSharp https://github.com/AngleSharp/AngleSharp to parse page links
	/// </summary>
	[Serializable]
	public class AngleSharpHyperlinkParser : HyperLinkParser
	{
		#region Consts
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		public const string c_PARSER_NAME = "AngleSharp";
		public const string c_HREF = "href";
		public const string c_LINK = "link";
		public const string c_A_and_AREA = "a, area";
		public const string c_BASE = "base";
		public const string c_META_NAME = "meta[name]";
		public const string c_NAME = "name";
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
		protected override string ParserType { get { return c_PARSER_NAME; } }

		#endregion

		#region Ctors

		/// <summary>
		/// Do nothing. Requared for serialization
		/// </summary>
		public AngleSharpHyperlinkParser() { }

		/// <summary>
		/// Create Crawl Configuration by input params
		/// </summary>
		/// <param name="isRespectMetaRobotsNoFollowEnabled">Whether parser should ignore pages with meta no robots</param>
		/// <param name="isRespectAnchorRelNoFollowEnabled">Whether parser should ignore links with rel no follow</param>
		/// <param name="cleanURLFunc">Function to clean the url</param>
		/// <param name="isRespectUrlNamedAnchorOrHashbangEnabled">Whether parser should consider named anchor and/or hashbang '#' character as part of the url</param>
		[Obsolete("Use the constructor that accepts a configuration object instead")]
		public AngleSharpHyperlinkParser(bool isRespectMetaRobotsNoFollowEnabled,
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
		public AngleSharpHyperlinkParser(CrawlConfiguration config, Func<string, string> cleanURLFunc)
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

			IEnumerable<string> hrefValues = crawledPage.AngleSharpHtmlDocument
				.QuerySelectorAll(c_A_and_AREA)
				.Where(e => !HasRelNoFollow(e))
				.Select(y => y.GetAttribute(c_HREF))
				.Where(a => !string.IsNullOrWhiteSpace(a));

			IEnumerable<string> canonicalHref = crawledPage.AngleSharpHtmlDocument
				.QuerySelectorAll(c_LINK)
				.Where(e => HasRelCanonicalPointingToDifferentUrl(e, crawledPage.Uri.ToString()))
				.Select(e => e.GetAttribute(c_HREF));

			return hrefValues.Concat(canonicalHref);
		}

		/// <summary>
		/// Get base url name
		/// </summary>
		/// <param name="crawledPage">Page for parsing</param>
		/// <returns>base url</returns>
		protected override string GetBaseHrefValue(CrawledPage crawledPage)
		{
			var baseTag = crawledPage.AngleSharpHtmlDocument
				.QuerySelector(c_BASE);

			if (baseTag == null)
				return string.Empty;

			var baseTagValue = baseTag.Attributes[c_HREF];
			if (baseTagValue == null)
				return string.Empty;

			return baseTagValue.Value?.Trim();
		}

		/// <summary>
		/// Get metadata content for robots value
		/// </summary>
		/// <param name="crawledPage">Page for parsing</param>
		/// <returns>Content for robots value</returns>
		protected override string GetMetaRobotsValue(CrawledPage crawledPage)
		{
			var robotsMeta = crawledPage.AngleSharpHtmlDocument
				.QuerySelectorAll(c_META_NAME)
				.FirstOrDefault(d => d.GetAttribute(c_NAME)
				.ToLowerInvariant() == c_ROBOTS);

			if (robotsMeta == null)
				return string.Empty;

			return robotsMeta.GetAttribute(c_CONTENT);
		}

		#endregion

		#region Protected Virtual Methods

		/// <summary>
		/// True, if element has "rel" attribute == "nofollow"
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		protected virtual bool HasRelNoFollow(IElement element)
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
		protected virtual bool HasRelCanonicalPointingToDifferentUrl(IElement element, string orginalUrl)
		{
			return element.HasAttribute(c_REL) &&
				   !string.IsNullOrWhiteSpace(element.GetAttribute(c_REL)) &&
				   string.Equals(element.GetAttribute(c_REL), c_CANONICAL, StringComparison.OrdinalIgnoreCase) &&
				   element.HasAttribute(c_HREF) && !string.IsNullOrWhiteSpace(element.GetAttribute(c_HREF)) &&
				   !string.Equals(element.GetAttribute(c_HREF), orginalUrl, StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}
}
