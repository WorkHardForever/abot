using System;
using System.Collections.Generic;
using Louw.SitemapParser;

namespace Abot.Core.Sitemap
{
	public interface IRobotsSitemap
	{
		Uri Location { get; }

		SitemapType SitemapType { get; }

		IEnumerable<IRobotsSitemap> Sitemaps { get; }

		IEnumerable<IRobotsSitemapItem> Items { get; }

		DateTime? LastModified { get; }

		bool IsLoaded { get; }
	}
}
