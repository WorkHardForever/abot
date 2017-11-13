using System;
using System.Threading;
using Abot.Poco;
using log4net;

namespace Abot.Util
{
	/// <summary>
	/// Handles the multithreading implementation details
	/// </summary>
	[Serializable]
	public abstract class ThreadManager : IThreadManager
	{
		#region Protected Fields

		protected ILog _logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);
		protected bool _abortAllCalled = false;
		protected int _numberOfRunningThreads = 0;
		protected ManualResetEvent _resetEvent = new ManualResetEvent(true);
		protected Object _locker = new Object();
		protected bool _isDisplosed = false;

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
				throw new ArgumentNullException("action");

			if (_abortAllCalled)
				throw new InvalidOperationException("Cannot call DoWork() after AbortAll() or Dispose() have been called.");

			if (!_isDisplosed && MaxThreads > 1)
			{
				_resetEvent.WaitOne();
				lock (_locker)
				{
					_numberOfRunningThreads++;
					if (!_isDisplosed && _numberOfRunningThreads >= MaxThreads)
						_resetEvent.Reset();

					_logger.DebugFormat("Starting another thread, increasing running threads to [{0}].", _numberOfRunningThreads);
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
			return _numberOfRunningThreads > 0;
		}

		/// <summary>
		/// Abort all running threads
		/// </summary>
		public virtual void AbortAll()
		{
			_abortAllCalled = true;
			_numberOfRunningThreads = 0;
		}

		public virtual void Dispose()
		{
			AbortAll();
			_resetEvent.Dispose();
			_isDisplosed = true;
		}

		#endregion

		#region Protected Methods

		protected virtual void RunAction(Action action, bool decrementRunningThreadCountOnCompletion = true)
		{
			try
			{
				action.Invoke();
				_logger.Debug("Action completed successfully.");
			}
			catch (OperationCanceledException)
			{
				_logger.DebugFormat("Thread cancelled.");
				throw;
			}
			catch (Exception e)
			{
				_logger.Error("Error occurred while running action.");
				_logger.Error(e);
			}
			finally
			{
				if (decrementRunningThreadCountOnCompletion)
				{
					lock (_locker)
					{
						_numberOfRunningThreads--;
						_logger.DebugFormat("[{0}] threads are running.", _numberOfRunningThreads);
						if (!_isDisplosed && _numberOfRunningThreads < MaxThreads)
							_resetEvent.Set();
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
