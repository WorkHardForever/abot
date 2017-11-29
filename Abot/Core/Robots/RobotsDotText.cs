using System;
using Abot.Poco;
using log4net;
using Robots;

namespace Abot.Core.Robots
{
	/// <summary>
	/// Wrapper for IRobot util
	/// </summary>
	[Serializable]
	public class RobotsDotText : IRobotsDotText
	{
		#region Protected Field

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		#endregion

		#region Ctor

		/// <summary>
		/// Wrap IRobots loading content
		/// </summary>
		/// <param name="rootUri">Absolute root uri</param>
		/// <param name="content">Content of robots.txt</param>
		public RobotsDotText(Uri rootUri, string content)
		{
			if (rootUri == null)
			{
				Logger.ErrorFormat("Argument null exception: \"{0}\" is null", nameof(rootUri));
				throw new ArgumentNullException(nameof(rootUri));
			}

			if (content == null)
			{
				Logger.ErrorFormat("Argument null exception: \"{0}\" is null", nameof(content));
				throw new ArgumentNullException(nameof(content));
			}

			RootUri = rootUri;
			Load(rootUri, content);
		}

		#endregion

		#region Public Variables

		/// <summary>
		/// Adapted class for parsing robots.txt
		/// </summary>
		public IRobots Robots { get; protected set; }

		/// <summary>
		/// Absolute root uri
		/// </summary>
		public Uri RootUri { get; protected set; }

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the number of seconds to delay between internal page crawls.
		/// Default: 0
		/// </summary>
		public int GetCrawlDelay(string userAgentString)
		{
			return Robots.GetCrawlDelay(userAgentString);
		}

		/// <summary>
		/// Whether the spider is "allowed" to crawl the param link
		/// </summary>
		public bool IsUrlAllowed(string url, string userAgentString)
		{
		    return !RootUri.IsBaseOf(new Uri(url)) ||
                    Robots.Allowed(url, userAgentString);
		}

		/// <summary>
		/// Whether the user agent is "allowed" to crawl the root url
		/// NOTE: "userAgentString" NOT EQUAL with User Agent from Crawl Config
		/// </summary>
		/// <param name="userAgentString"></param>
		/// <returns>True if find such user agent in content</returns>
		public bool IsUserAgentAllowed(string userAgentString)
		{
		    return !string.IsNullOrEmpty(userAgentString) &&
                    Robots.Allowed(RootUri.AbsoluteUri, userAgentString);
		}

		#endregion

		#region Protected Method

		/// <summary>
		/// Load content to IRobots
		/// </summary>
		/// <param name="rootUri"></param>
		/// <param name="content"></param>
		protected void Load(Uri rootUri, string content)
		{
			Robots = new global::Robots.Robots();
			Robots.LoadContent(content, rootUri.AbsoluteUri);
		}

		#endregion
	}
}
