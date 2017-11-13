using System;

namespace Abot.Util
{
	public interface IMemoryMonitor : IDisposable
    {
        int GetCurrentUsageInMb();
    }
}
