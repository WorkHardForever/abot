using System;
using System.Collections.Generic;
using Abot.Poco;
using HtmlAgilityPack;

namespace Abot.Core.Parsers
{
    /// <summary>
    /// Parser that uses Html Agility Pack http://htmlagilitypack.codeplex.com/ to parse page links
    /// </summary>
    [Serializable]
    public class HapHyperLinkParser : HyperLinkParser
    {
        #region Consts
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public const string ParserName = "HtmlAgilityPack";
        public const string Href = "href";
        public const string Link = "link";
        public const string AandArea = "a, area";
        public const string Base = "base";
        public const string MetaName = "meta[name]";
        public const string Name = "name";
        public const string Robots = "robots";
        public const string Content = "content";
        public const string Rel = "rel";
        public const string Canonical = "canonical";

        public const string NodeA = "//a[@href]";
        public const string NodeArea = "//area[@href]";
        public const string NodeLink = "//link[@rel='canonical'][@href]";
        public const string NodeBase = "//base";
        public const string NodeRobots = "//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='robots']";

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        #endregion

        #region Protected Field

        /// <summary>
        /// Requare for logger information. Parser name can be equal as name of your derived class
        /// </summary>
        protected override string ParserType => ParserName;

        #endregion

        #region Ctors

        /// <summary>
        /// Create with empty configuration
        /// </summary>
        public HapHyperLinkParser() { }

        /// <summary>
        /// Create Crawl Configuration by input params
        /// </summary>
        /// <param name="isRespectMetaRobotsNoFollowEnabled">Whether parser should ignore pages with meta no robots</param>
        /// <param name="isRespectAnchorRelNoFollowEnabled">Whether parser should ignore links with rel no follow</param>
        /// <param name="cleanUrlFunc">Function to clean the url</param>
        /// <param name="isRespectUrlNamedAnchorOrHashbangEnabled">Whether parser should consider named anchor and/or hashbang '#' character as part of the url</param>
        [Obsolete("Use the constructor that accepts a configuration object instead")]
        public HapHyperLinkParser(bool isRespectMetaRobotsNoFollowEnabled,
                                  bool isRespectAnchorRelNoFollowEnabled,
                                  Func<string, string> cleanUrlFunc = null,
                                  bool isRespectUrlNamedAnchorOrHashbangEnabled = false)
            : this(new CrawlConfiguration
            {
                IsRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled,
                IsRespectUrlNamedAnchorOrHashbangEnabled = isRespectUrlNamedAnchorOrHashbangEnabled,
                IsRespectAnchorRelNoFollowEnabled = isRespectAnchorRelNoFollowEnabled
            }, cleanUrlFunc)
        { }

        /// <summary>
        /// Create Crawl Configuration by input params
        /// </summary>
        /// <param name="config">CrawlConfiguration</param>
        /// <param name="cleanUrlFunc"></param>
        public HapHyperLinkParser(CrawlConfiguration config, Func<string, string> cleanUrlFunc)
            : base(config, cleanUrlFunc)
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

            HtmlNodeCollection aTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes(NodeA);
            HtmlNodeCollection areaTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes(NodeArea);
            HtmlNodeCollection canonicals = crawledPage.HtmlDocument.DocumentNode.SelectNodes(NodeLink);

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
            HtmlNode node = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode(NodeBase);
            string hrefValue = string.Empty;

            //Must use node.InnerHtml instead of node.InnerText since "aaa<br />bbb" will be returned as "aaabbb"
            if (node != null)
                hrefValue = node.GetAttributeValue(Href, string.Empty).Trim();

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
                .SelectSingleNode(NodeRobots);

            if (robotsNode != null)
                robotsMeta = robotsNode.GetAttributeValue(Content, string.Empty);

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
            HtmlAttribute attribute = node.Attributes[Rel];
            return Config.IsRespectAnchorRelNoFollowEnabled &&
                   (attribute != null &&
                    attribute.Value.ToLower().Trim() == NoFollow);
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

                string hrefValue = node.Attributes[Href].Value;

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
                Logger.InfoFormat("Error dentitizing uri: {0} This usually means that it contains unexpected characters", hrefValue);
            }

            return dentitizedHref;
        }

        #endregion
    }
}
