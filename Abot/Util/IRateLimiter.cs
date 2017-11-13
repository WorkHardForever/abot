namespace Abot.Util
{
	/// <summary>
	/// Used to control the rate of some occurrence per unit of time.
	/// </summary>
	/// <remarks>
	///     <para>
	///			To control the rate of an action using a <see cref="RateLimiter"/>, 
	///			code should simply call <see cref="WaitToProceed()"/> prior to 
	///			performing the action. <see cref="WaitToProceed()"/> will block
	///			the current thread until the action is allowed based on the rate 
	///			limit.
	///     </para>
	///     <para>
	///			This class is thread safe. A single <see cref="RateLimiter"/> instance 
	///			may be used to control the rate of an occurrence across multiple 
	///			threads.
	///     </para>
	/// </remarks>
	public interface IRateLimiter
	{
		/// <summary>
		/// Blocks the current thread indefinitely until allowed to proceed.
		/// </summary>
		void WaitToProceed();
	}
}
