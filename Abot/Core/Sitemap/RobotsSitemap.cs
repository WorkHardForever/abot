using System;
using System.Collections.Generic;
using System.Linq;
using Louw.SitemapParser;

namespace Abot.Core.Sitemap
{
	public class RobotsSitemap : IRobotsSitemap
	{
		private readonly Louw.SitemapParser.Sitemap AdapteeObject;

		public RobotsSitemap(Louw.SitemapParser.Sitemap adapteeObject)
		{
			AdapteeObject = adapteeObject;
		}

		public RobotsSitemap(Uri sitemapLocation, DateTime? lastModified = null)
		{
			AdapteeObject = new Louw.SitemapParser.Sitemap(sitemapLocation, lastModified);
		}

		public RobotsSitemap(IEnumerable<Louw.SitemapParser.Sitemap> sitemaps, Uri sitemapLocation = null, DateTime? lastModified = null)
		{
			AdapteeObject = new Louw.SitemapParser.Sitemap(sitemaps, sitemapLocation, lastModified);
		}

		public RobotsSitemap(IEnumerable<SitemapItem> items, Uri sitemapLocation = null, DateTime? lastModified = null)
		{
			AdapteeObject = new Louw.SitemapParser.Sitemap(items, sitemapLocation, lastModified);
		}

		public Uri Location => AdapteeObject.SitemapLocation;

		public SitemapType SitemapType => AdapteeObject.SitemapType;

		public IEnumerable<IRobotsSitemap> Sitemaps =>
			AdapteeObject.Sitemaps.Select(x => new RobotsSitemap(x));

		public IEnumerable<IRobotsSitemapItem> Items =>
			AdapteeObject.Items.Select(x => new RobotsSitemapItem(x));

		public DateTime? LastModified => AdapteeObject.LastModified;

		public bool IsLoaded => AdapteeObject.IsLoaded;

		public static Louw.SitemapParser.Sitemap MapIRobotsSitemapToSitemap(IRobotsSitemap robotsSitemap)
		{
			return (robotsSitemap as RobotsSitemap)?.AdapteeObject;
		}
	}
}
