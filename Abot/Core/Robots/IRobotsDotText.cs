using Robots;

namespace Abot.Core
{
	/// <summary>
	/// Wrapper for IRobot util
	/// </summary>
	public interface IRobotsDotText
	{
		/// <summary>
		/// Instance of robot.txt object
		/// </summary>
		IRobots Robots { get; }

		/// <summary>
		/// Gets the number of seconds to delay between internal page crawls. Returns 0 by default.
		/// </summary>
		int GetCrawlDelay(string userAgentString);

		/// <summary>
		/// Whether the spider is "allowed" to crawl the param link
		/// </summary>
		bool IsUrlAllowed(string url, string userAgentString);

		/// <summary>
		/// Whether the user agent is "allowed" to crawl the root url
		/// </summary>
		bool IsUserAgentAllowed(string userAgentString);
	}
}
