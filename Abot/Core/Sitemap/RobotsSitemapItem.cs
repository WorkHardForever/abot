using System;
using Louw.SitemapParser;

namespace Abot.Core.Sitemap
{
	public class RobotsSitemapItem : IRobotsSitemapItem
	{
		private readonly SitemapItem AdapteeObject;

		public RobotsSitemapItem(SitemapItem adapteeObject)
		{
			AdapteeObject = adapteeObject;
		}

		public Uri Location => AdapteeObject.Location;

		public DateTime? LastModified => AdapteeObject.LastModified;

		public RobotsSitemapChangeFrequency? ChangeFrequency =>
			(RobotsSitemapChangeFrequency?)(int?)AdapteeObject.ChangeFrequency;

		public double? Priority => AdapteeObject.Priority;

		public static SitemapItem MapIRobotsSitemapToSitemap(IRobotsSitemapItem robotsSitemapItem)
		{
			return (robotsSitemapItem as RobotsSitemapItem)?.AdapteeObject;
		}
	}
}
