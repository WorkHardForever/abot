using System;

namespace Abot.Util
{
	/// <summary>
	/// Handles memory monitoring/usage
	/// </summary>
	public interface IMemoryManager : IMemoryMonitor
	{
		/// <summary>
		/// Whether the current process that is hosting this instance is allocated/using above the param value of memory in mb
		/// </summary>
		bool IsCurrentUsageAbove(int sizeInMb);

		/// <summary>
		/// Whether there is at least the param value of available memory in mb
		/// </summary>
		bool IsSpaceAvailable(int sizeInMb);
	}
}
