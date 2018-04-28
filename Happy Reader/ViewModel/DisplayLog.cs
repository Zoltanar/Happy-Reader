using System;
using Happy_Reader.Database;

namespace Happy_Reader.ViewModel
{
	public class DisplayLog
	{
		public DateTime Timestamp { get; }
		public string Type { get; }
		public string Message { get; }

		public DisplayLog(Log log)
		{
			Timestamp = log.Timestamp;
			Type = log.Kind.ToString();
			Message = log.ToString();
		}


	}
}
