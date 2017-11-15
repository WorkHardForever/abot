using System;

namespace Abot.Core.Sitemap
{
	public interface IRobotsSitemapItem
	{
		Uri Location { get; }

		DateTime? LastModified { get; }

		RobotsSitemapChangeFrequency? ChangeFrequency { get; }

		double? Priority { get; }
	}
}
