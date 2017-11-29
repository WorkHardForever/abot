using System;
using Louw.SitemapParser;

namespace Abot.Core.Sitemap
{
	public class RobotsSitemapItem : IRobotsSitemapItem
	{
		private readonly SitemapItem _adapteeObject;

		public RobotsSitemapItem(SitemapItem adapteeObject)
		{
			_adapteeObject = adapteeObject;
		}

		public Uri Location => _adapteeObject.Location;

		public DateTime? LastModified => _adapteeObject.LastModified;

		public RobotsSitemapChangeFrequency? ChangeFrequency =>
			(RobotsSitemapChangeFrequency?)(int?)_adapteeObject.ChangeFrequency;

		public double? Priority => _adapteeObject.Priority;

		public static SitemapItem MapIRobotsSitemapToSitemap(IRobotsSitemapItem robotsSitemapItem)
		{
			return (robotsSitemapItem as RobotsSitemapItem)?._adapteeObject;
		}
	}
}
