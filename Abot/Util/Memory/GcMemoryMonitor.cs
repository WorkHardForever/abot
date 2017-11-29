using System;
using System.Diagnostics;
using Abot.Poco;
using log4net;

namespace Abot.Util
{
	[Serializable]
	public class GcMemoryMonitor : IMemoryMonitor
	{
	    private static ILog _logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		public virtual int GetCurrentUsageInMb()
		{
			Stopwatch timer = Stopwatch.StartNew();
			int currentUsageInMb = Convert.ToInt32(GC.GetTotalMemory(false) / (1024 * 1024));
			timer.Stop();

			_logger.DebugFormat("GC reporting [{0}mb] currently thought to be allocated, took [{1}] millisecs", currentUsageInMb, timer.ElapsedMilliseconds);

			return currentUsageInMb;
		}

		public void Dispose()
		{
			//do nothing
		}
	}
}
