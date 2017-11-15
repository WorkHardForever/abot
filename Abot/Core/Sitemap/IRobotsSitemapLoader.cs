using System;

namespace Abot.Core.Sitemap
{
	public interface IRobotsSitemapLoader
	{
		IRobotsSitemap Load(Uri sitemapLocation);

		IRobotsSitemap Load(IRobotsSitemap sitemap);
	}
}
