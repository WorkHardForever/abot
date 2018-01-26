using Abot.Poco;

namespace Abot.Core.Config.Mappers
{
	/// <summary>
	/// Simple mapping
	/// </summary>
	public static class CrawlConfigurationMapperExtension
	{
		#region Extension Methods

		/// <summary>
		/// Map Behavior element to config
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="source"></param>
		public static void ImportCrawlBehaviorElement(this CrawlConfiguration destination, CrawlBehaviorElement source)
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

		/// <summary>
		/// Map Politeness element to config
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="source"></param>
		public static void ImportPolitenessElement(this CrawlConfiguration destination, PolitenessElement source)
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

		/// <summary>
		/// Map Authorization element to config
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="source"></param>
		public static void ImportAuthorizationElement(this CrawlConfiguration destination, AuthorizationElement source)
		{
			destination.IsAlwaysLogin = source.IsAlwaysLogin;
			destination.LoginUser = source.LoginUser;
			destination.LoginPassword = source.LoginPassword;
		}

		/// <summary>
		/// Map Extension Value element to config
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="source"></param>
		public static void ImportExtensionValueCollection(this CrawlConfiguration destination, ExtensionValueCollection source)
		{
			foreach (ExtensionValueElement element in source)
				destination.ConfigurationExtensions.Add(element.Key, element.Value);
		}

		#endregion
	}
}
