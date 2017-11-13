using System;
using System.Text;

namespace Abot.Poco
{
	/// <summary>
	/// Content from getting response
	/// </summary>
	[Serializable]
	public class PageContent
	{
		#region Ctor

		/// <summary>
		/// Set base configuration
		/// </summary>
		public PageContent()
		{
			Text = string.Empty;
		}

		#endregion

		#region Public Variables

		/// <summary>
		/// The raw data bytes taken from the web response
		/// </summary>
		public byte[] Bytes { get; set; }

		/// <summary>
		/// String representation of the charset/encoding
		/// </summary>
		public string Charset { get; set; }

		/// <summary>
		/// The encoding of the web response
		/// </summary>
		public Encoding Encoding { get; set; }

		/// <summary>
		/// The raw text taken from the web response
		/// </summary>
		public string Text { get; set; }

		#endregion
	}
}
