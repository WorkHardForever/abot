namespace Abot.Utils.Time
{
	/// <summary>
	/// Convert time values
	/// </summary>
	public class TimeConverter
	{
		#region Const

		/// <summary>
		/// Value for translation seconds to milliseconds
		/// </summary>
		public const int MillisecondTranslation = 1000;

		#endregion

		#region Public Static Methods

		/// <summary>
		/// Convert seconds to milliseconds
		/// </summary>
		/// <param name="seconds"></param>
		/// <returns></returns>
		public static long SecondsToMilliseconds(long seconds)
		{
			if (seconds <= 0)
				return 0;

			return MillisecondTranslation * seconds;
		}

		#endregion
	}
}
