﻿using System;
using System.Timers;
using Abot.Poco;
using Abot.Utils.Time;
using log4net;

namespace Abot.Utils.Memory
{
	[Serializable]
	public class CachedMemoryMonitor : IMemoryMonitor
	{
	    private static ILog _logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);
	    private IMemoryMonitor _memoryMonitor;
	    private Timer _usageRefreshTimer;
	    private int _cachedCurrentUsageInMb;

		public CachedMemoryMonitor(IMemoryMonitor memoryMonitor, int cacheExpirationInSeconds)
		{
			if (memoryMonitor == null)
				throw new ArgumentNullException(nameof(memoryMonitor));

			if (cacheExpirationInSeconds < 1)
				cacheExpirationInSeconds = 5;

			_memoryMonitor = memoryMonitor;

			UpdateCurrentUsageValue();

			_usageRefreshTimer = new Timer(TimeConverter.SecondsToMilliseconds(cacheExpirationInSeconds));
			_usageRefreshTimer.Elapsed += (sender, e) => UpdateCurrentUsageValue();
			_usageRefreshTimer.Start();
		}

		protected virtual void UpdateCurrentUsageValue()
		{
			int oldUsage = _cachedCurrentUsageInMb;
			_cachedCurrentUsageInMb = _memoryMonitor.GetCurrentUsageInMb();
			_logger.DebugFormat("Updated cached memory usage value from [{0}mb] to [{1}mb]", oldUsage, _cachedCurrentUsageInMb);
		}

		public virtual int GetCurrentUsageInMb()
		{
			return _cachedCurrentUsageInMb;
		}

		public void Dispose()
		{
			_usageRefreshTimer.Stop();
			_usageRefreshTimer.Dispose();
		}
	}
}
