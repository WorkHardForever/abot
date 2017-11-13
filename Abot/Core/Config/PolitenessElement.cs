using System;
using System.Configuration;

namespace Abot.Core
{
	/// <summary>
	/// Set configuration for using robots capabilities
	/// </summary>
	[Serializable]
	public class PolitenessElement : ConfigurationElement
	{
		#region Public Configuration Properies

		/// <summary>
		/// Whether the crawler should retrieve and respect the robots.txt file.
		/// </summary>
		[ConfigurationProperty("isRespectRobotsDotTextEnabled", IsRequired = false)]
		public bool IsRespectRobotsDotTextEnabled { get { return (bool)this["isRespectRobotsDotTextEnabled"]; } }

		/// <summary>
		/// Whether the crawler should ignore links on pages that have a <meta name="robots" content="nofollow" /> tag
		/// </summary>
		[ConfigurationProperty("isRespectMetaRobotsNoFollowEnabled", IsRequired = false)]
		public bool IsRespectMetaRobotsNoFollowEnabled { get { return (bool)this["isRespectMetaRobotsNoFollowEnabled"]; } }

		/// <summary>
		/// Whether the crawler should ignore links on pages that have an http X-Robots-Tag header of nofollow
		/// </summary>
		[ConfigurationProperty("isRespectHttpXRobotsTagHeaderNoFollowEnabled", IsRequired = false)]
		public bool IsRespectHttpXRobotsTagHeaderNoFollowEnabled { get { return (bool)this["isRespectHttpXRobotsTagHeaderNoFollowEnabled"]; } }

		/// <summary>
		/// Whether the crawler should ignore links that have a <a href="whatever" rel="nofollow"></a>
		/// </summary>
		[ConfigurationProperty("isRespectAnchorRelNoFollowEnabled", IsRequired = false)]
		public bool IsRespectAnchorRelNoFollowEnabled { get { return (bool)this["isRespectAnchorRelNoFollowEnabled"]; } }

		/// <summary>
		/// If true, will ignore the robots.txt file if it disallows crawling the root uri.
		/// </summary>
		[ConfigurationProperty("isIgnoreRobotsDotTextIfRootDisallowedEnabled", IsRequired = false)]
		public bool IsIgnoreRobotsDotTextIfRootDisallowedEnabled { get { return (bool)this["isIgnoreRobotsDotTextIfRootDisallowedEnabled"]; } }

		/// <summary>
		/// The user agent string to use when checking robots.txt file for specific directives.
		/// Some examples of other crawler's user agent values are "googlebot", "slurp" etc...
		/// Default: "abot"
		/// </summary>
		[ConfigurationProperty("robotsDotTextUserAgentString", IsRequired = false, DefaultValue = "abot")]
		public string RobotsDotTextUserAgentString { get { return (string)this["robotsDotTextUserAgentString"]; } }

		/// <summary>
		/// The number of milliseconds to wait in between http requests to the same domain.
		/// Default: 5
		/// </summary>
		[ConfigurationProperty("maxRobotsDotTextCrawlDelayInSeconds", IsRequired = false, DefaultValue = 5)]
		public int MaxRobotsDotTextCrawlDelayInSeconds { get { return (int)this["maxRobotsDotTextCrawlDelayInSeconds"]; } }

		/// <summary>
		/// The maximum numer of seconds to respect in the robots.txt "Crawl-delay: X" directive. 
		/// IsRespectRobotsDotTextEnabled must be true for this value to be used.
		/// If zero, will use whatever the robots.txt crawl delay requests no matter how high the value is.
		/// </summary>
		[ConfigurationProperty("minCrawlDelayPerDomainMilliSeconds", IsRequired = false)]
		public int MinCrawlDelayPerDomainMilliSeconds { get { return (int)this["minCrawlDelayPerDomainMilliSeconds"]; } }

		#endregion
	}
}
