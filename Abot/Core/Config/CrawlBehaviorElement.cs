using System;
using System.Configuration;

namespace Abot.Core.Config
{
	/// <summary>
	/// Set basic configuration for crawler
	/// </summary>
	[Serializable]
	public class CrawlBehaviorElement : ConfigurationElement
	{
		#region Public Configuration Properies

		/// <summary>
		/// Max concurrent threads to use for http requests
		/// Default: 10
		/// </summary>
		[ConfigurationProperty("maxConcurrentThreads", IsRequired = false, DefaultValue = 10)]
		public int MaxConcurrentThreads => (int)this["maxConcurrentThreads"];

		/// <summary>
		/// Maximum number of pages to crawl. 
		/// If zero, this setting has no effect
		/// Default: 1000
		/// </summary>
		[ConfigurationProperty("maxPagesToCrawl", IsRequired = false, DefaultValue = 1000)]
		public int MaxPagesToCrawl => (int)this["maxPagesToCrawl"];

		/// <summary>
		/// Maximum number of pages to crawl per domain
		/// If zero, this setting has no effect.
		/// </summary>
		[ConfigurationProperty("maxPagesToCrawlPerDomain", IsRequired = false)]
		public int MaxPagesToCrawlPerDomain => (int)this["maxPagesToCrawlPerDomain"];

		/// <summary>
		/// Maximum size of page. If the page size is above this value, it will not be downloaded or processed
		/// If zero, this setting has no effect.
		/// </summary>
		[ConfigurationProperty("maxPageSizeInBytes", IsRequired = false)]
		public int MaxPageSizeInBytes => (int)this["maxPageSizeInBytes"];

		/// <summary>
		/// The user agent string to use for http requests
		/// Default: "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko"
		/// </summary>
		[ConfigurationProperty("userAgentString", IsRequired = false, DefaultValue = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko")]
		public string UserAgentString => (string)this["userAgentString"];

		/// <summary>
		/// Maximum seconds before the crawl times out and stops. 
		/// If zero, this setting has no effect.
		/// </summary>
		[ConfigurationProperty("crawlTimeoutSeconds", IsRequired = false)]
		public int CrawlTimeoutSeconds => (int)this["crawlTimeoutSeconds"];

		/// <summary>
		/// A comma seperated string that has content types that should have their page content downloaded.
		/// For each page, the content type is checked to see if it contains any of the values defined here.
		/// </summary>
		[ConfigurationProperty("downloadableContentTypes", IsRequired = false, DefaultValue = "text/html")]
		public string DownloadableContentTypes => (string)this["downloadableContentTypes"];

		/// <summary>
		/// Whether Uris should be crawled more than once. This is not common and should be false for most scenarios
		/// </summary>
		[ConfigurationProperty("isUriRecrawlingEnabled", IsRequired = false)]
		public bool IsUriRecrawlingEnabled => (bool)this["isUriRecrawlingEnabled"];

		/// <summary>
		/// Whether pages external to the root uri should be crawled
		/// </summary>
		[ConfigurationProperty("isExternalPageCrawlingEnabled", IsRequired = false)]
		public bool IsExternalPageCrawlingEnabled => (bool)this["isExternalPageCrawlingEnabled"];

		/// <summary>
		/// Whether pages external to the root uri should have their links crawled.
		/// NOTE: IsExternalPageCrawlEnabled must be TRUE for this setting to have any effect
		/// </summary>
		[ConfigurationProperty("isExternalPageLinksCrawlingEnabled", IsRequired = false)]
		public bool IsExternalPageLinksCrawlingEnabled => (bool)this["isExternalPageLinksCrawlingEnabled"];

		/// <summary>
		/// Whether or not url named anchors or hashbangs are considered part of the url.
		/// If false, they will be ignored. If true, they will be considered part of the url.
		/// </summary>
		[ConfigurationProperty("isRespectUrlNamedAnchorOrHashbangEnabled", IsRequired = false)]
		public bool IsRespectUrlNamedAnchorOrHashbangEnabled => (bool)this["isRespectUrlNamedAnchorOrHashbangEnabled"];

		/// <summary>
		/// Gets or sets the maximum number of concurrent connections allowed by a System.Net.ServicePoint.
		/// Default: 2. This means that only 2 concurrent http connections can be open to the same host.
		/// If zero, this setting has no effect.
		/// </summary>
		[ConfigurationProperty("httpServicePointConnectionLimit", IsRequired = false, DefaultValue = 200)]
		public int HttpServicePointConnectionLimit => (int)this["httpServicePointConnectionLimit"];

		/// <summary>
		/// Gets or sets the time-out value in milliseconds for the System.Net.HttpWebRequest.GetResponse()
		/// and System.Net.HttpWebRequest.GetRequestStream() methods.
		/// If zero, this setting has no effect.
		/// Default: 15
		/// </summary>
		[ConfigurationProperty("httpRequestTimeoutInSeconds", IsRequired = false, DefaultValue = 15)]
		public int HttpRequestTimeoutInSeconds => (int)this["httpRequestTimeoutInSeconds"];

		/// <summary>
		/// Gets or sets the maximum number of redirects that the request follows.
		/// If zero, this setting has no effect.
		/// Default: 7
		/// </summary>
		[ConfigurationProperty("httpRequestMaxAutoRedirects", IsRequired = false, DefaultValue = 7)]
		public int HttpRequestMaxAutoRedirects => (int)this["httpRequestMaxAutoRedirects"];

		/// <summary>
		/// Gets or sets a value that indicates whether the request should follow redirection
		/// Default: true
		/// </summary>
		[ConfigurationProperty("isHttpRequestAutoRedirectsEnabled", IsRequired = false, DefaultValue = true)]
		public bool IsHttpRequestAutoRedirectsEnabled => (bool)this["isHttpRequestAutoRedirectsEnabled"];

		/// <summary>
		/// Gets or sets a value that indicates GZIP and DEFLATE will be automatically accepted and decompressed
		/// </summary>
		[ConfigurationProperty("isHttpRequestAutomaticDecompressionEnabled", IsRequired = false)]
		public bool IsHttpRequestAutomaticDecompressionEnabled => (bool)this["isHttpRequestAutomaticDecompressionEnabled"];

		/// <summary>
		/// Whether the cookies should be set and resent with every request
		/// </summary>
		[ConfigurationProperty("isSendingCookiesEnabled", IsRequired = false)]
		public bool IsSendingCookiesEnabled => (bool)this["isSendingCookiesEnabled"];

		/// <summary>
		/// Whether or not to validate the server SSL certificate. If true, the validation will be made.
		/// If false, the certificate validation is bypassed. This setting is useful to crawl sites with an
		/// invalid or expired SSL certificate.
		/// Default: True
		/// </summary>
		[ConfigurationProperty("isSslCertificateValidationEnabled", IsRequired = false, DefaultValue = true)]
		public bool IsSslCertificateValidationEnabled => (bool)this["isSslCertificateValidationEnabled"];

		/// <summary>
		/// Uses closest mulitple of 16 to the value set. If there isn't at least this much memory available
		/// before starting a crawl, throws InsufficientMemoryException.
		/// If zero, this setting has no effect.
		/// </summary>
		/// <exception>
		/// InsufficientMemoryException http://msdn.microsoft.com/en-us/library/system.insufficientmemoryexception.aspx
		/// </exception>
		[ConfigurationProperty("minAvailableMemoryRequiredInMb", IsRequired = false)]
		public int MinAvailableMemoryRequiredInMb => (int)this["minAvailableMemoryRequiredInMb"];

		/// <summary>
		/// The max amount of memory to allow the process to use.
		/// If this limit is exceeded the crawler will stop prematurely.
		/// If zero, this setting has no effect.
		/// </summary>
		[ConfigurationProperty("maxMemoryUsageInMb", IsRequired = false)]
		public int MaxMemoryUsageInMb => (int)this["maxMemoryUsageInMb"];

		/// <summary>
		/// The max amount of time before refreshing the value used to determine the amount of memory
		/// being used by the process that hosts the crawler instance.
		/// If MaxMemoryUsageInMb is zero, this value has no effect.
		/// </summary>
		[ConfigurationProperty("maxMemoryUsageCacheTimeInSeconds", IsRequired = false)]
		public int MaxMemoryUsageCacheTimeInSeconds => (int)this["maxMemoryUsageCacheTimeInSeconds"];

		/// <summary>
		/// Maximum levels below root page to crawl.
		/// If value is 0, the homepage will be crawled but none of its links will be crawled.
		/// If the level is 1, the homepage and its links will be crawled but none of the links will be crawled.
		/// Default: 100
		/// </summary>
		[ConfigurationProperty("maxCrawlDepth", IsRequired = false, DefaultValue = 100)]
		public int MaxCrawlDepth => (int)this["maxCrawlDepth"];

		/// <summary>
		/// Maximum links to crawl per page.
		/// If value is zero, this setting has no effect.
		/// Default: 0
		/// </summary>
		[ConfigurationProperty("maxLinksPerPage", IsRequired = false, DefaultValue = 0)]
		public int MaxLinksPerPage => (int)this["maxLinksPerPage"];

		/// <summary>
		/// Gets or sets a value that indicates whether the crawler should parse the page's links even
		/// if a CrawlDecision (like CrawlDecisionMaker.ShouldCrawlPageLinks()) determines that those
		/// links will not be crawled.
		/// </summary>
		[ConfigurationProperty("isForcedLinkParsingEnabled", IsRequired = false)]
		public bool IsForcedLinkParsingEnabled => (bool)this["isForcedLinkParsingEnabled"];

		/// <summary>
		/// The max number of retries for a url if a web exception is encountered.
		/// If the value is 0, no retries will be made
		/// </summary>
		[ConfigurationProperty("maxRetryCount", IsRequired = false)]
		public int MaxRetryCount => (int)this["maxRetryCount"];

		/// <summary>
		/// The minimum delay between a failed http request and the next retry
		/// </summary>
		[ConfigurationProperty("minRetryDelayInMilliseconds", IsRequired = false)]
		public int MinRetryDelayInMilliseconds => (int)this["minRetryDelayInMilliseconds"];

		#endregion
	}
}
