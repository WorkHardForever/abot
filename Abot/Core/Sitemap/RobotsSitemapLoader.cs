using System;
using Louw.SitemapParser;

namespace Abot.Core.Sitemap
{
	public class RobotsSitemapLoader : IRobotsSitemapLoader
	{
		private readonly SitemapLoader AdapteeObject;

		public RobotsSitemapLoader(ISitemapFetcher fetcher = null, ISitemapParser sitemapParser = null, IRobotsTxtParser robotsParser = null)
		{
			AdapteeObject = new SitemapLoader(fetcher, sitemapParser, robotsParser);
		}

		public IRobotsSitemap Load(Uri sitemapLocation)
			=> new RobotsSitemap(AdapteeObject.LoadAsync(sitemapLocation).Result);

		public IRobotsSitemap Load(IRobotsSitemap sitemap)
			=> new RobotsSitemap(AdapteeObject.LoadAsync(RobotsSitemap.MapIRobotsSitemapToSitemap(sitemap)).Result);
	}
}
