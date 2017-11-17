using System;
using System.Net;
using Abot.Poco;
using log4net;

namespace Abot.Core
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
		public const string c_ROBOTS_TXT = "/robots.txt";

		#endregion

		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog _logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		/// <summary>
		/// Make request to page for getting content
		/// </summary>
		protected IPageRequester _pageRequester;

		#endregion

		#region Ctor

		/// <summary>
		/// Set start configuration
		/// </summary>
		/// <param name="pageRequester"></param>
		public RobotsDotTextFinder(IPageRequester pageRequester)
		{
			if (pageRequester == null)
				throw new ArgumentNullException(nameof(pageRequester));

			_pageRequester = pageRequester;
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

			Uri robotsUri = new Uri(rootUri, c_ROBOTS_TXT);

			// If should crawl site not from start page
			if (!robotsUri.ToString().Contains(rootUri.ToString()))
			{
				_logger.DebugFormat("Your url couldn't have robots.txt");
				return null;
			}

			CrawledPage page = _pageRequester.MakeRequest(robotsUri);

			if (page == null ||
				page.WebException != null ||
				page.HttpWebResponse == null ||
				page.HttpWebResponse.StatusCode != HttpStatusCode.OK)
			{
				_logger.DebugFormat("Did not find robots.txt file at [{0}]", robotsUri);
				return null;
			}

			_logger.DebugFormat("Found robots.txt file at [{0}]", robotsUri);
			return new RobotsDotText(rootUri, page.Content.Text);
		}

		#endregion
	}
}
