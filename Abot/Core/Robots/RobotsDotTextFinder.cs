using System;
using System.Net;
using Abot.Poco;
using log4net;

namespace Abot.Core.Robots
{
	/// <summary>
	/// Finds and builds the robots.txt file abstraction
	/// </summary>
	[Serializable]
	public class RobotsDotTextFinder : IRobotsDotTextFinder
	{
		#region Const

		/// <summary>
		/// Link to getting robots.txt
		/// </summary>
		public const string RobotsTxt = "/robots.txt";

		#endregion

		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		/// <summary>
		/// Make request to page for getting content
		/// </summary>
		protected IPageRequester PageRequester;

		#endregion

		#region Ctor

		/// <summary>
		/// Set start configuration
		/// </summary>
		/// <param name="pageRequester"></param>
		public RobotsDotTextFinder(IPageRequester pageRequester)
		{
            PageRequester = pageRequester ?? throw new ArgumentNullException(nameof(pageRequester));
		}

		#endregion

		#region Public Method

		/// <summary>
		/// Finds the robots.txt file using the rootUri. 
		/// If rootUri is http://yahoo.com, it will look for robots at http://yahoo.com/robots.txt.
		/// If rootUri is http://music.yahoo.com, it will look for robots at http://music.yahoo.com/robots.txt
		/// </summary>
		/// <param name="rootUri">The root domain</param>
		/// <returns>Object representing the robots.txt file or returns null</returns>
		public IRobotsDotText Find(Uri rootUri)
		{
			if (rootUri == null)
				throw new ArgumentNullException(nameof(rootUri));

			Uri robotsUri = new Uri(rootUri, RobotsTxt);

			// If should crawl site not from start page
			if (!robotsUri.ToString().Contains(rootUri.ToString()))
			{
				Logger.DebugFormat("Your url couldn't have robots.txt");
				return null;
			}

			CrawledPage page = PageRequester.MakeRequest(robotsUri);

			if (page == null ||
				page.WebException != null ||
				page.HttpWebResponse == null ||
				page.HttpWebResponse.StatusCode != HttpStatusCode.OK)
			{
				Logger.DebugFormat("Did not find robots.txt file at [{0}]", robotsUri);
				return null;
			}

			Logger.DebugFormat("Found robots.txt file at [{0}]", robotsUri);
			return new RobotsDotText(rootUri, page.Content.Text);
		}

		#endregion
	}
}
