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
		static ILog _logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);
		IMemoryMonitor _memoryMonitor;

		public MemoryManager(IMemoryMonitor memoryMonitor)
		{
			if (memoryMonitor == null)
				throw new ArgumentNullException("memoryMonitor");

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

			MemoryFailPoint _memoryFailPoint = null;
			try
			{
				_memoryFailPoint = new MemoryFailPoint(sizeInMb);
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
				if (_memoryFailPoint != null)
					_memoryFailPoint.Dispose();
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
