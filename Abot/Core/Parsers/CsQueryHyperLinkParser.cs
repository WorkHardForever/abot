﻿using System;
using System.Collections.Generic;
using System.Linq;
using Abot.Poco;
using CsQuery;

namespace Abot.Core.Parsers
{
    /// <summary>
    /// Parser that uses CsQuery https://github.com/jamietre/CsQuery to parse page links
    /// </summary>
    [Serializable]
    [Obsolete("CSQuery is no longer actively maintained. Use AngleSharpHyperlinkParser for similar usage/functionality")]
    public class CsQueryHyperlinkParser : HyperLinkParser
    {
        #region Consts
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public const string ParserName = "CsQuery";
        public const string Href = "href";
        public const string Link = "link";
        public const string AandArea = "a, area";
        public const string Base = "base";
        public const string MetaName = "meta[name]";
        public const string Robots = "robots";
        public const string Content = "content";
        public const string Rel = "rel";
        public const string Canonical = "canonical";

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
        public CsQueryHyperlinkParser() { }

        /// <summary>
        /// Create Crawl Configuration by input params
        /// </summary>
        /// <param name="isRespectMetaRobotsNoFollowEnabled">Whether parser should ignore pages with meta no robots</param>
        /// <param name="isRespectAnchorRelNoFollowEnabled">Whether parser should ignore links with rel no follow</param>
        /// <param name="cleanUrlFunc">Function to clean the url</param>
        /// <param name="isRespectUrlNamedAnchorOrHashbangEnabled">Whether parser should consider named anchor and/or hashbang '#' character as part of the url</param>
        [Obsolete("Use the constructor that accepts a configuration object instead")]
        public CsQueryHyperlinkParser(bool isRespectMetaRobotsNoFollowEnabled,
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
        public CsQueryHyperlinkParser(CrawlConfiguration config, Func<string, string> cleanUrlFunc)
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

            IEnumerable<string> hrefValues = crawledPage.CsQueryDocument
                .Select(AandArea)
                .Elements
                .Where(e => !HasRelNoFollow(e))
                .Select(y => y.GetAttribute(Href))
                .Where(a => !string.IsNullOrWhiteSpace(a));

            IEnumerable<string> canonicalHref = crawledPage.CsQueryDocument
                .Select(Link)
                .Elements
                .Where(e => HasRelCanonicalPointingToDifferentUrl(e, crawledPage.Uri.ToString()))
                .Select(e => e.Attributes[Href]);

            return hrefValues.Concat(canonicalHref);
        }

        /// <summary>
        /// Get base url name
        /// </summary>
        /// <param name="crawledPage">Page for parsing</param>
        /// <returns>base url</returns>
        protected override string GetBaseHrefValue(CrawledPage crawledPage)
        {
            string baseTagValue = crawledPage.CsQueryDocument.Select(Base).Attr(Href) ?? string.Empty;
            return baseTagValue.Trim();
        }

        /// <summary>
        /// Get metadata content for robots value
        /// </summary>
        /// <param name="crawledPage">Page for parsing</param>
        /// <returns>Content for robots value</returns>
        protected override string GetMetaRobotsValue(CrawledPage crawledPage)
        {
            return crawledPage.CsQueryDocument[MetaName]
                .Filter(d => d.Name.ToLowerInvariant() == Robots)
                .Attr(Content);
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
            return Config.IsRespectAnchorRelNoFollowEnabled &&
                   element.HasAttribute(Rel) &&
                   element.GetAttribute(Rel).ToLower().Trim() == NoFollow;
        }

        /// <summary>
        /// Has "rel" canonical pointing To different url
        /// </summary>
        /// <param name="element"></param>
        /// <param name="orginalUrl"></param>
        /// <returns></returns>
        protected virtual bool HasRelCanonicalPointingToDifferentUrl(IDomElement element, string orginalUrl)
        {
            return element.HasAttribute(Rel) &&
                   !string.IsNullOrWhiteSpace(element.Attributes[Rel]) &&
                   string.Equals(element.Attributes[Rel], Canonical, StringComparison.OrdinalIgnoreCase) &&
                   element.HasAttribute(Href) &&
                   !string.IsNullOrWhiteSpace(element.Attributes[Href]) &&
                   !string.Equals(element.Attributes[Href], orginalUrl, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
