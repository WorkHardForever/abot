using System;
using Louw.SitemapParser;

namespace Abot.Core.Sitemap
{
	public class RobotsSitemapLoader : IRobotsSitemapLoader
	{
		private readonly SitemapLoader _adapteeObject;

		public RobotsSitemapLoader(ISitemapFetcher fetcher = null, ISitemapParser sitemapParser = null, IRobotsTxtParser robotsParser = null)
		{
			_adapteeObject = new SitemapLoader(fetcher, sitemapParser, robotsParser);
		}

		public IRobotsSitemap Load(Uri sitemapLocation)
			=> new RobotsSitemap(_adapteeObject.LoadAsync(sitemapLocation).Result);

		public IRobotsSitemap Load(IRobotsSitemap sitemap)
			=> new RobotsSitemap(_adapteeObject.LoadAsync(RobotsSitemap.MapIRobotsSitemapToSitemap(sitemap)).Result);
	}
}
