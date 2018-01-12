using System;
using Abot.Crawler.EventArgs;

namespace Abot.Crawler.Interfaces
{
	/// <summary>
	/// Polite web crawler
	/// </summary>
	public interface IPoliteWebCrawler : IWebCrawler
	{
		/// <summary>
		/// Event occur after robots txt is parsed asynchroniously
		/// </summary>
		event EventHandler<RobotsDotTextParseCompletedEventArgs> RobotsDotTextParseCompletedAsync;
		/// <summary>
		/// Event occur after robots txt is parsed synchroniously
		/// </summary>
		event EventHandler<RobotsDotTextParseCompletedEventArgs> RobotsDotTextParseCompleted;
	}
}
