using Abot.Poco;
using Robots;

namespace Abot.Crawler.EventArgs
{
    /// <summary>
    /// Class which hold robot txt data after successful parsing
    /// </summary>
    public class RobotsDotTextParseCompletedEventArgs : CrawlEventArgs
    {
        /// <summary>
        /// robots.txt object
        /// </summary>
        public IRobots Robots { get; set; }

        /// <summary>
        /// Contructor to be used to create an object which will path arugments when robots txt is parsed
        /// </summary>
        /// <param name="crawlContext"></param>
        /// <param name="robots"></param>
        public RobotsDotTextParseCompletedEventArgs(CrawlContext crawlContext, IRobots robots) : base(crawlContext)
        {
            Robots = robots;
        }
    }
}
