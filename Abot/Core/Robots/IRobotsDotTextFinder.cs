using System;

namespace Abot.Core.Robots
{
	/// <summary>
	/// Finds and builds the robots.txt file abstraction
	/// </summary>
	public interface IRobotsDotTextFinder
	{
		/// <summary>
		/// Finds the robots.txt file using the rootUri. 
		/// If rootUri is http://yahoo.com, it will look for robots at http://yahoo.com/robots.txt.
		/// If rootUri is http://music.yahoo.com, it will look for robots at http://music.yahoo.com/robots.txt
		/// </summary>
		/// <param name="rootUri">The root domain</param>
		/// <returns>Object representing the robots.txt file or returns null</returns>
		IRobotsDotText Find(Uri rootUri);
	}
}
