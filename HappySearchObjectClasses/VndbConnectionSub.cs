namespace Happy_Apps_Core
{
	partial class VndbConnection
	{
		public enum MessageSeverity { Normal, Warning, Error }
		private enum LogInStatus { No, Yes }
		public enum ApiStatus
		{
			Ready,
			Busy,
			Error,
			Closed
		}
	}
}
