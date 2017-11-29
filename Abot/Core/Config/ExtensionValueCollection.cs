using System;
using System.Configuration;

namespace Abot.Core.Config
{
	/// <summary>
	/// Collection of extension elements from extension section
	/// </summary>
	[Serializable]
	public class ExtensionValueCollection : ConfigurationElementCollection
	{
		#region Indexator

		/// <summary>
		/// Get ExtensionValueElement by index from config file
		/// </summary>
		/// <param name="index">int index</param>
		/// <returns>finded ExtensionValueElement by index</returns>
		public ExtensionValueElement this[int index] => (ExtensionValueElement)BaseGet(index);

		#endregion

		#region Protected Override Methods

		/// <summary>
		/// Create new ExtensionValueElement
		/// </summary>
		/// <returns>ExtensionValueElement</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new ExtensionValueElement();
		}

		/// <summary>
		/// Get key from getting element
		/// </summary>
		/// <param name="element">ExtensionValueElement</param>
		/// <returns>string Key</returns>
		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ExtensionValueElement)element).Key;
		}

		#endregion
	}
}
