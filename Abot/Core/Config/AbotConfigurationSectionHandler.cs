using System;
using System.Configuration;
using Abot.Poco;

namespace Abot.Core
{
	/// <summary>
	/// Set configuration from app.config file
	/// </summary>
	[Serializable]
	public class AbotConfigurationSectionHandler : ConfigurationSection
	{
		#region Const

		/// <summary>
		/// Section of app.config with all credentials for crawler
		/// </summary>
		public const string c_CONFIG_SECTION = "abot";

		#endregion

		#region Ctor

		/// <summary>
		/// Do nothing. Requared for serialization
		/// </summary>
		public AbotConfigurationSectionHandler() { }

		#endregion

		#region Public Configuration Properies

		/// <summary>
		/// Set basic configuration for crawler
		/// </summary>
		[ConfigurationProperty("crawlBehavior")]
		public CrawlBehaviorElement CrawlBehavior { get { return (CrawlBehaviorElement)this["crawlBehavior"]; } }

		/// <summary>
		/// Set configuration for using robots capabilities
		/// </summary>
		[ConfigurationProperty("politeness")]
		public PolitenessElement Politeness { get { return (PolitenessElement)this["politeness"]; } }

		/// <summary>
		/// Set configuration for using Sign On in site for crawl
		/// </summary>
		[ConfigurationProperty("authorization")]
		public AuthorizationElement Authorization { get { return (AuthorizationElement)this["authorization"]; } }

		/// <summary>
		/// Collection of extension elements from extension section
		/// </summary>
		[ConfigurationProperty("extensionValues")]
		[ConfigurationCollection(typeof(ExtensionValueCollection), AddItemName = "add")]
		public ExtensionValueCollection ExtensionValues { get { return (ExtensionValueCollection)this["extensionValues"]; } }

		#endregion

		#region Public Methods

		/// <summary>
		/// Generate from all own fields to CrawlConfiguration
		/// </summary>
		/// <returns>Crawl Configuration</returns>
		public CrawlConfiguration Convert()
		{
			CrawlConfiguration config = new CrawlConfiguration();

			Map(CrawlBehavior, config);
			Map(Politeness, config);
			Map(Authorization, config);
			Map(ExtensionValues, config);

			return config;
		}

		/// <summary>
		/// Loading AbotConfigurationSectionHandler from section "abot" in app file
		/// </summary>
		/// <returns>Abot Configuration Section Handler</returns>
		public static AbotConfigurationSectionHandler LoadFromXml()
		{
			return (System.Configuration.ConfigurationManager.GetSection(c_CONFIG_SECTION) as AbotConfigurationSectionHandler);
		}

		#endregion

		#region Private Methods

		private void Map(CrawlBehaviorElement source, CrawlConfiguration destination)
		{
			destination.MaxConcurrentThreads = source.MaxConcurrentThreads;
			destination.MaxPagesToCrawl = source.MaxPagesToCrawl;
			destination.MaxPagesToCrawlPerDomain = source.MaxPagesToCrawlPerDomain;
			destination.MaxPageSizeInBytes = source.MaxPageSizeInBytes;
			destination.UserAgentString = source.UserAgentString;
			destination.CrawlTimeoutSeconds = source.CrawlTimeoutSeconds;
			destination.IsUriRecrawlingEnabled = source.IsUriRecrawlingEnabled;
			destination.IsExternalPageCrawlingEnabled = source.IsExternalPageCrawlingEnabled;
			destination.IsExternalPageLinksCrawlingEnabled = source.IsExternalPageLinksCrawlingEnabled;
			destination.IsRespectUrlNamedAnchorOrHashbangEnabled = source.IsRespectUrlNamedAnchorOrHashbangEnabled;
			destination.DownloadableContentTypes = source.DownloadableContentTypes;
			destination.HttpServicePointConnectionLimit = source.HttpServicePointConnectionLimit;
			destination.HttpRequestTimeoutInSeconds = source.HttpRequestTimeoutInSeconds;
			destination.HttpRequestMaxAutoRedirects = source.HttpRequestMaxAutoRedirects;
			destination.IsHttpRequestAutoRedirectsEnabled = source.IsHttpRequestAutoRedirectsEnabled;
			destination.IsHttpRequestAutomaticDecompressionEnabled = source.IsHttpRequestAutomaticDecompressionEnabled;
			destination.IsSendingCookiesEnabled = source.IsSendingCookiesEnabled;
			destination.IsSslCertificateValidationEnabled = source.IsSslCertificateValidationEnabled;
			destination.MinAvailableMemoryRequiredInMb = source.MinAvailableMemoryRequiredInMb;
			destination.MaxMemoryUsageInMb = source.MaxMemoryUsageInMb;
			destination.MaxMemoryUsageCacheTimeInSeconds = source.MaxMemoryUsageCacheTimeInSeconds;
			destination.MaxCrawlDepth = source.MaxCrawlDepth;
			destination.MaxLinksPerPage = source.MaxLinksPerPage;
			destination.IsForcedLinkParsingEnabled = source.IsForcedLinkParsingEnabled;
			destination.MaxRetryCount = source.MaxRetryCount;
			destination.MinRetryDelayInMilliseconds = source.MinRetryDelayInMilliseconds;
		}

		private void Map(PolitenessElement source, CrawlConfiguration destination)
		{
			destination.IsRespectRobotsDotTextEnabled = source.IsRespectRobotsDotTextEnabled;
			destination.IsRespectMetaRobotsNoFollowEnabled = source.IsRespectMetaRobotsNoFollowEnabled;
			destination.IsRespectHttpXRobotsTagHeaderNoFollowEnabled = source.IsRespectHttpXRobotsTagHeaderNoFollowEnabled;
			destination.IsRespectAnchorRelNoFollowEnabled = source.IsRespectAnchorRelNoFollowEnabled;
			destination.IsIgnoreRobotsDotTextIfRootDisallowedEnabled = source.IsIgnoreRobotsDotTextIfRootDisallowedEnabled;
			destination.RobotsDotTextUserAgentString = source.RobotsDotTextUserAgentString;
			destination.MinCrawlDelayPerDomainMilliSeconds = source.MinCrawlDelayPerDomainMilliSeconds;
			destination.MaxRobotsDotTextCrawlDelayInSeconds = source.MaxRobotsDotTextCrawlDelayInSeconds;
		}

		private void Map(AuthorizationElement source, CrawlConfiguration destination)
		{
			destination.IsAlwaysLogin = source.IsAlwaysLogin;
			destination.LoginUser = source.LoginUser;
			destination.LoginPassword = source.LoginPassword;
		}

		private void Map(ExtensionValueCollection source, CrawlConfiguration destination)
		{
			foreach (ExtensionValueElement element in source)
				destination.ConfigurationExtensions.Add(element.Key, element.Value);
		}

		#endregion
	}
}
