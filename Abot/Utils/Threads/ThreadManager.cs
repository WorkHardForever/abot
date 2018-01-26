using System;
using System.Threading;
using Abot.Poco;
using log4net;

namespace Abot.Utils
{
	/// <summary>
	/// Handles the multithreading implementation details
	/// </summary>
	[Serializable]
	public abstract class ThreadManager : IThreadManager
	{
		#region Protected Fields

		protected ILog Logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);
		protected bool AbortAllCalled = false;
		protected int NumberOfRunningThreads = 0;
		protected ManualResetEvent ResetEvent = new ManualResetEvent(true);
		protected Object Locker = new Object();
		protected bool IsDisplosed = false;

		#endregion

		#region Ctor

		public ThreadManager(int maxThreads)
		{
			if ((maxThreads < 1) || (100 < maxThreads))
				throw new ArgumentException("MaxThreads must be from 1 to 100");

			MaxThreads = maxThreads;
		}

		#endregion

		#region Public Variables

		/// <summary>
		/// Max number of threads to use
		/// </summary>
		public int MaxThreads { get; set; }

		#endregion

		#region Public Methods

		/// <summary>
		/// Will perform the action asynchrously on a seperate thread
		/// </summary>
		/// <param name="action">The action to perform</param>
		public virtual void DoWork(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (AbortAllCalled)
				throw new InvalidOperationException("Cannot call DoWork() after AbortAll() or Dispose() have been called.");

			if (!IsDisplosed && MaxThreads > 1)
			{
				ResetEvent.WaitOne();
				lock (Locker)
				{
					NumberOfRunningThreads++;
					if (!IsDisplosed && NumberOfRunningThreads >= MaxThreads)
						ResetEvent.Reset();

					Logger.DebugFormat("Starting another thread, increasing running threads to [{0}].", NumberOfRunningThreads);
				}
				RunActionOnDedicatedThread(action);
			}
			else
			{
				RunAction(action, false);
			}
		}

		/// <summary>
		/// Whether there are running threads
		/// </summary>
		public virtual bool HasRunningThreads()
		{
			return NumberOfRunningThreads > 0;
		}

		/// <summary>
		/// Abort all running threads
		/// </summary>
		public virtual void AbortAll()
		{
			AbortAllCalled = true;
			NumberOfRunningThreads = 0;
		}

		public virtual void Dispose()
		{
			AbortAll();
			ResetEvent.Dispose();
			IsDisplosed = true;
		}

		#endregion

		#region Protected Methods

		protected virtual void RunAction(Action action, bool decrementRunningThreadCountOnCompletion = true)
		{
			try
			{
				action.Invoke();
				Logger.Debug("Action completed successfully.");
			}
			catch (OperationCanceledException)
			{
				Logger.DebugFormat("Thread cancelled.");
				throw;
			}
			catch (Exception e)
			{
				Logger.Error("Error occurred while running action.");
				Logger.Error(e);
			}
			finally
			{
				if (decrementRunningThreadCountOnCompletion)
				{
					lock (Locker)
					{
						NumberOfRunningThreads--;
						Logger.DebugFormat("[{0}] threads are running.", NumberOfRunningThreads);
						if (!IsDisplosed && NumberOfRunningThreads < MaxThreads)
							ResetEvent.Set();
					}
				}
			}
		}

		/// <summary>
		/// Runs the action on a seperate thread
		/// </summary>
		protected abstract void RunActionOnDedicatedThread(Action action);

		#endregion
	}
}
