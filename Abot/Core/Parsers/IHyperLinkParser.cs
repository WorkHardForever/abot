using System;
using System.Collections.Generic;
using Abot.Poco;

namespace Abot.Core
{
	/// <summary>
	/// Handles parsing hyperlinks out of the raw html
	/// </summary>
	public interface IHyperLinkParser
	{
		/// <summary>
		/// Parses html to extract hyperlinks, converts each into an absolute url
		/// </summary>
		IEnumerable<Uri> GetLinks(CrawledPage crawledPage);
	}
}
