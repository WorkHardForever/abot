using System;
using System.Collections.Generic;
using Abot.Poco;
using HtmlAgilityPack;

namespace Abot.Core
{
	/// <summary>
	/// Parser that uses Html Agility Pack http://htmlagilitypack.codeplex.com/ to parse page links
	/// </summary>
	[Serializable]
	public class HapHyperLinkParser : HyperLinkParser
	{
		#region Consts
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		public const string c_PARSER_NAME = "HtmlAgilityPack";
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

		public const string c_NODE_A = "//a[@href]";
		public const string c_NODE_AREA = "//area[@href]";
		public const string c_NODE_LINK = "//link[@rel='canonical'][@href]";
		public const string c_NODE_BASE = "//base";
		public const string c_NODE_ROBOTS = "//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='robots']";

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
		/// Create with empty configuration
		/// </summary>
		public HapHyperLinkParser()
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
		public HapHyperLinkParser(bool isRespectMetaRobotsNoFollowEnabled,
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
		public HapHyperLinkParser(CrawlConfiguration config, Func<string, string> cleanURLFunc)
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

			HtmlNodeCollection aTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes(c_NODE_A);
			HtmlNodeCollection areaTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes(c_NODE_AREA);
			HtmlNodeCollection canonicals = crawledPage.HtmlDocument.DocumentNode.SelectNodes(c_NODE_LINK);

			List<string> hrefValues = new List<string>();

			hrefValues.AddRange(GetLinks(aTags));
			hrefValues.AddRange(GetLinks(areaTags));
			hrefValues.AddRange(GetLinks(canonicals));

			return hrefValues;
		}

		/// <summary>
		/// Get base url name
		/// </summary>
		/// <param name="crawledPage">Page for parsing</param>
		/// <returns>base url</returns>
		protected override string GetBaseHrefValue(CrawledPage crawledPage)
		{
			HtmlNode node = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode(c_NODE_BASE);
			string hrefValue = string.Empty;

			//Must use node.InnerHtml instead of node.InnerText since "aaa<br />bbb" will be returned as "aaabbb"
			if (node != null)
				hrefValue = node.GetAttributeValue(c_HREF, string.Empty).Trim();

			return hrefValue;
		}

		/// <summary>
		/// Get metadata content for robots value
		/// </summary>
		/// <param name="crawledPage">Page for parsing</param>
		/// <returns>Content for robots value</returns>
		protected override string GetMetaRobotsValue(CrawledPage crawledPage)
		{
			string robotsMeta = string.Empty;
			HtmlNode robotsNode = crawledPage.HtmlDocument.DocumentNode
				.SelectSingleNode(c_NODE_ROBOTS);

			if (robotsNode != null)
				robotsMeta = robotsNode.GetAttributeValue(c_CONTENT, string.Empty);

			return robotsMeta;
		}

		#endregion

		#region Protected Virtual Methods

		/// <summary>
		/// True, if element has "rel" attribute == "nofollow"
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected virtual bool HasRelNoFollow(HtmlNode node)
		{
			HtmlAttribute attribute = node.Attributes[c_REL];
			return _config.IsRespectAnchorRelNoFollowEnabled &&
				   (attribute != null &&
					attribute.Value.ToLower().Trim() == c_NOFOLLOW);
		}

		/// <summary>
		/// Generate from html nodes list of links
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		protected virtual List<string> GetLinks(HtmlNodeCollection nodes)
		{
			List<string> hrefs = new List<string>();

			if (nodes == null)
				return hrefs;

			foreach (HtmlNode node in nodes)
			{
				if (HasRelNoFollow(node))
					continue;

				string hrefValue = node.Attributes[c_HREF].Value;
				if (!string.IsNullOrWhiteSpace(hrefValue))
				{
					hrefValue = DeEntitize(hrefValue);
					hrefs.Add(hrefValue);
				}
			}

			return hrefs;
		}

		/// <summary>
		/// Replace known entities by characters
		/// </summary>
		/// <param name="hrefValue">href</param>
		/// <returns>DeEntitize string href</returns>
		protected virtual string DeEntitize(string hrefValue)
		{
			string dentitizedHref = hrefValue;

			try
			{
				dentitizedHref = HtmlEntity.DeEntitize(hrefValue);
			}
			catch (Exception)
			{
				_logger.InfoFormat("Error dentitizing uri: {0} This usually means that it contains unexpected characters", hrefValue);
			}

			return dentitizedHref;
		}

		#endregion
	}
}
