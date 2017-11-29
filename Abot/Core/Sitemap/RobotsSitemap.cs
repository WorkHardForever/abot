using System;
using System.Collections.Generic;
using System.Linq;
using Louw.SitemapParser;

namespace Abot.Core.Sitemap
{
	public class RobotsSitemap : IRobotsSitemap
	{
		private readonly Louw.SitemapParser.Sitemap _adapteeObject;

		public RobotsSitemap(Louw.SitemapParser.Sitemap adapteeObject)
		{
			_adapteeObject = adapteeObject;
		}

		public RobotsSitemap(Uri sitemapLocation, DateTime? lastModified = null)
		{
			_adapteeObject = new Louw.SitemapParser.Sitemap(sitemapLocation, lastModified);
		}

		public RobotsSitemap(IEnumerable<Louw.SitemapParser.Sitemap> sitemaps, Uri sitemapLocation = null, DateTime? lastModified = null)
		{
			_adapteeObject = new Louw.SitemapParser.Sitemap(sitemaps, sitemapLocation, lastModified);
		}

		public RobotsSitemap(IEnumerable<SitemapItem> items, Uri sitemapLocation = null, DateTime? lastModified = null)
		{
			_adapteeObject = new Louw.SitemapParser.Sitemap(items, sitemapLocation, lastModified);
		}

		public Uri Location => _adapteeObject.SitemapLocation;

		public SitemapType SitemapType => _adapteeObject.SitemapType;

		public IEnumerable<IRobotsSitemap> Sitemaps =>
			_adapteeObject.Sitemaps.Select(x => new RobotsSitemap(x));

		public IEnumerable<IRobotsSitemapItem> Items =>
			_adapteeObject.Items.Select(x => new RobotsSitemapItem(x));

		public DateTime? LastModified => _adapteeObject.LastModified;

		public bool IsLoaded => _adapteeObject.IsLoaded;

		public static Louw.SitemapParser.Sitemap MapIRobotsSitemapToSitemap(IRobotsSitemap robotsSitemap)
		{
			return (robotsSitemap as RobotsSitemap)?._adapteeObject;
		}
	}
}
