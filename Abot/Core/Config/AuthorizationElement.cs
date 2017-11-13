using System;
using System.Configuration;

namespace Abot.Core
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
		public bool IsAlwaysLogin { get { return (bool)this["isAlwaysLogin"]; } }

		/// <summary>
		/// The user name to be used for autorization 
		/// </summary>
		[ConfigurationProperty("loginUser", IsRequired = false)]
		public string LoginUser { get { return (string)this["loginUser"]; } }

		/// <summary>
		/// The password to be used for autorization 
		/// </summary>
		[ConfigurationProperty("loginPassword", IsRequired = false)]
		public string LoginPassword { get { return (string)this["loginPassword"]; } }

		#endregion
	}
}
