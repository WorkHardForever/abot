using System;
using System.Net;
using Abot.Poco;

namespace Abot.Core
{
	/// <summary>
	/// Extractor for generating info from requested page
	/// </summary>
	public interface IWebContentExtractor
    {
		/// <summary>
		/// Collect data from responsed page
		/// </summary>
		/// <param name="response">Web response</param>
		/// <returns>Extract page</returns>
        PageContent GetContent(WebResponse response);
    }
}
