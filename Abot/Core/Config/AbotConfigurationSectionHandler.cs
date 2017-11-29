using System;
using System.Configuration;
using Abot.Core.Config.Mappers;
using Abot.Poco;

namespace Abot.Core.Config
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
		public const string ConfigSection = "abot";

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
		public CrawlBehaviorElement CrawlBehavior => (CrawlBehaviorElement)this["crawlBehavior"];

		/// <summary>
		/// Set configuration for using robots capabilities
		/// </summary>
		[ConfigurationProperty("politeness")]
		public PolitenessElement Politeness => (PolitenessElement)this["politeness"];

		/// <summary>
		/// Set configuration for using Sign On in site for crawl
		/// </summary>
		[ConfigurationProperty("authorization")]
		public AuthorizationElement Authorization => (AuthorizationElement)this["authorization"];

		/// <summary>
		/// Collection of extension elements from extension section
		/// </summary>
		[ConfigurationProperty("extensionValues")]
		[ConfigurationCollection(typeof(ExtensionValueCollection), AddItemName = "add")]
		public ExtensionValueCollection ExtensionValues => (ExtensionValueCollection)this["extensionValues"];

		#endregion

		#region Public Methods

		/// <summary>
		/// Generate from all own fields to CrawlConfiguration
		/// </summary>
		/// <returns>Crawl Configuration</returns>
		public CrawlConfiguration Convert()
		{
			CrawlConfiguration config = new CrawlConfiguration();

			config.ImportCrawlBehaviorElement(CrawlBehavior);
			config.ImportPolitenessElement(Politeness);
			config.ImportAuthorizationElement(Authorization);
			config.ImportExtensionValueCollection(ExtensionValues);

			return config;
		}

		/// <summary>
		/// Loading AbotConfigurationSectionHandler from section "abot" in app file
		/// </summary>
		/// <returns>Abot Configuration Section Handler</returns>
		public static AbotConfigurationSectionHandler LoadFromXml() =>
			System.Configuration.ConfigurationManager.GetSection(ConfigSection) as AbotConfigurationSectionHandler;

		#endregion
	}
}
