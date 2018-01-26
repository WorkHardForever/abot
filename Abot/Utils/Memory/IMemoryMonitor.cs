using System;

namespace Abot.Utils.Memory
{
	public interface IMemoryMonitor : IDisposable
    {
        int GetCurrentUsageInMb();
    }
}
