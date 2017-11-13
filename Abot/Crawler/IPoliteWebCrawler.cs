using System;

namespace Abot.Crawler
{
	/// <summary>
	/// Polite web crawler
	/// </summary>
	public interface IPoliteWebCrawler : IWebCrawler
	{
		/// <summary>
		/// Event occur after robots txt is parsed asynchroniously
		/// </summary>
		event EventHandler<RobotsDotTextParseCompletedArgs> RobotsDotTextParseCompletedAsync;
		/// <summary>
		/// Event occur after robots txt is parsed synchroniously
		/// </summary>
		event EventHandler<RobotsDotTextParseCompletedArgs> RobotsDotTextParseCompleted;
	}
}
