using System;
using System.Configuration;

namespace Abot.Core.Config
{
	/// <summary>
	/// Set configuration for using Sign On in site for crawl
	/// </summary>
	[Serializable]
	public class AuthorizationElement : ConfigurationElement
	{
		#region Public Configuration Properies

		/// <summary>
		/// Defines whatewer each request shold be autorized via login 
		/// </summary>
		[ConfigurationProperty("isAlwaysLogin", IsRequired = false)]
		public bool IsAlwaysLogin => (bool)this["isAlwaysLogin"];

		/// <summary>
		/// The user name to be used for autorization 
		/// </summary>
		[ConfigurationProperty("loginUser", IsRequired = false)]
		public string LoginUser => (string)this["loginUser"];

		/// <summary>
		/// The password to be used for autorization 
		/// </summary>
		[ConfigurationProperty("loginPassword", IsRequired = false)]
		public string LoginPassword => (string)this["loginPassword"];

		#endregion
	}
}
