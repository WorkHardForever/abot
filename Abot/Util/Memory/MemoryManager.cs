using System;
using System.Runtime;
using Abot.Poco;
using log4net;

namespace Abot.Util
{
	/// <summary>
	/// Handles memory monitoring/usage
	/// </summary>
	[Serializable]
	public class MemoryManager : IMemoryManager
	{
	    private static ILog _logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);
	    private IMemoryMonitor _memoryMonitor;

		public MemoryManager(IMemoryMonitor memoryMonitor)
		{
			if (memoryMonitor == null)
				throw new ArgumentNullException(nameof(memoryMonitor));

			_memoryMonitor = memoryMonitor;
		}

		public virtual bool IsCurrentUsageAbove(int sizeInMb)
		{
			return GetCurrentUsageInMb() > sizeInMb;
		}

		public virtual bool IsSpaceAvailable(int sizeInMb)
		{
			if (sizeInMb < 1)
				return true;

			bool isAvailable = true;

			MemoryFailPoint memoryFailPoint = null;
			try
			{
				memoryFailPoint = new MemoryFailPoint(sizeInMb);
			}
			catch (InsufficientMemoryException)
			{
				isAvailable = false;
			}
			catch (NotImplementedException)
			{
				_logger.Warn("MemoryFailPoint is not implemented on this platform. The MemoryManager.IsSpaceAvailable() will just return true.");
			}
			finally
			{
				if (memoryFailPoint != null)
					memoryFailPoint.Dispose();
			}

			return isAvailable;
		}

		public virtual int GetCurrentUsageInMb()
		{
			return _memoryMonitor.GetCurrentUsageInMb();
		}

		public void Dispose()
		{
			_memoryMonitor.Dispose();
		}
	}
}
