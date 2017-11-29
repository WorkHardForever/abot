using System;
using System.Collections.Generic;
using System.Linq;
using Abot.Poco;
using AngleSharp.Dom;

namespace Abot.Core.Parsers
{
    /// <summary>
    /// Parser that uses AngleSharp https://github.com/AngleSharp/AngleSharp to parse page links
    /// </summary>
    [Serializable]
    public class AngleSharpHyperlinkParser : HyperLinkParser
    {
        #region Consts
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public const string ParserName = "AngleSharp";
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
        /// Do nothing. Requared for serialization
        /// </summary>
        public AngleSharpHyperlinkParser() { }

        /// <summary>
        /// Create Crawl Configuration by input params
        /// </summary>
        /// <param name="isRespectMetaRobotsNoFollowEnabled">Whether parser should ignore pages with meta no robots</param>
        /// <param name="isRespectAnchorRelNoFollowEnabled">Whether parser should ignore links with rel no follow</param>
        /// <param name="cleanUrlFunc">Function to clean the url</param>
        /// <param name="isRespectUrlNamedAnchorOrHashbangEnabled">Whether parser should consider named anchor and/or hashbang '#' character as part of the url</param>
        [Obsolete("Use the constructor that accepts a configuration object instead")]
        public AngleSharpHyperlinkParser(bool isRespectMetaRobotsNoFollowEnabled,
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
        public AngleSharpHyperlinkParser(CrawlConfiguration config, Func<string, string> cleanUrlFunc)
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

            IEnumerable<string> hrefValues = crawledPage.AngleSharpHtmlDocument
                .QuerySelectorAll(AandArea)
                .Where(e => !HasRelNoFollow(e))
                .Select(y => y.GetAttribute(Href))
                .Where(a => !string.IsNullOrWhiteSpace(a));

            IEnumerable<string> canonicalHref = crawledPage.AngleSharpHtmlDocument
                .QuerySelectorAll(Link)
                .Where(e => HasRelCanonicalPointingToDifferentUrl(e, crawledPage.Uri.ToString()))
                .Select(e => e.GetAttribute(Href));

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
                .QuerySelector(Base);

            if (baseTag == null)
                return string.Empty;

            var baseTagValue = baseTag.Attributes[Href];
            return baseTagValue == null ?
                string.Empty :
                baseTagValue.Value?.Trim();
        }

        /// <summary>
        /// Get metadata content for robots value
        /// </summary>
        /// <param name="crawledPage">Page for parsing</param>
        /// <returns>Content for robots value</returns>
        protected override string GetMetaRobotsValue(CrawledPage crawledPage)
        {
            var robotsMeta = crawledPage.AngleSharpHtmlDocument
                .QuerySelectorAll(MetaName)
                .FirstOrDefault(d => d.GetAttribute(Name)
                .ToLowerInvariant() == Robots);

            return robotsMeta == null ?
                string.Empty :
                robotsMeta.GetAttribute(Content);
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
            return Config.IsRespectAnchorRelNoFollowEnabled &&
                   (element.HasAttribute(Rel) &&
                    element.GetAttribute(Rel).ToLower().Trim() == NoFollow);
        }

        /// <summary>
        /// Has "rel" canonical pointing To different url
        /// </summary>
        /// <param name="element"></param>
        /// <param name="orginalUrl"></param>
        /// <returns></returns>
        protected virtual bool HasRelCanonicalPointingToDifferentUrl(IElement element, string orginalUrl)
        {
            return element.HasAttribute(Rel) &&
                   !string.IsNullOrWhiteSpace(element.GetAttribute(Rel)) &&
                   string.Equals(element.GetAttribute(Rel), Canonical, StringComparison.OrdinalIgnoreCase) &&
                   element.HasAttribute(Href) && !string.IsNullOrWhiteSpace(element.GetAttribute(Href)) &&
                   !string.Equals(element.GetAttribute(Href), orginalUrl, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
