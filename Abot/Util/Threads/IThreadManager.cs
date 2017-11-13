using System;

namespace Abot.Util
{
	/// <summary>
	/// Handles the multithreading implementation details
	/// </summary>
	public interface IThreadManager : IDisposable
	{
		/// <summary>
		/// Max number of threads to use.
		/// </summary>
		int MaxThreads { get; set; }

		/// <summary>
		/// Will perform the action asynchrously on a seperate thread
		/// </summary>
		/// <param name="action">The action to perform</param>
		void DoWork(Action action);

		/// <summary>
		/// Whether there are running threads
		/// </summary>
		bool HasRunningThreads();

		/// <summary>
		/// Abort all running threads
		/// </summary>
		void AbortAll();
	}
}
