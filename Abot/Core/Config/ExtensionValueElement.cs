using System;
using System.Configuration;

namespace Abot.Core.Config
{
	/// <summary>
	/// Extension section
	/// </summary>
	[Serializable]
	public class ExtensionValueElement : ConfigurationElement
	{
		#region Public Configuration Properies

		/// <summary>
		/// Key
		/// </summary>
		[ConfigurationProperty("key", IsRequired = false, IsKey = true)]
		public string Key => (string)this["key"];

		/// <summary>
		/// Value of Key
		/// </summary>
		[ConfigurationProperty("value", IsRequired = false, IsKey = false)]
		public string Value => (string)this["value"];

		#endregion
	}
}
