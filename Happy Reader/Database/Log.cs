using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace Happy_Reader.Database
{
	public class Log
	{
		private object _parsedData;

		public Log()
		{
			Timestamp = DateTime.UtcNow;
		}

		public long Id { get; set; }
		public LogKind Kind { get; set; }
		public long AssociatedId { get; set; }
		public string Data { get; set; }

		[NotMapped]
		public object ParsedData
		{
			get
			{
				if (_parsedData != null) return _parsedData;
				switch (Kind)
				{
					case LogKind.TimePlayed:
						_parsedData = TimeSpan.Parse(Data);
						break;
					case LogKind.StartedPlaying:
						_parsedData = DateTime.Parse(Data);
						break;
					default:
						_parsedData = Data;
						break;
				}
				return _parsedData;
			}
			set => _parsedData = value;
		}

		public DateTime Timestamp { get; set; }

		public delegate void LogNotificationEventHandler(Log log);

		public static event LogNotificationEventHandler NotificationEvent;
		public static event Action<Log> AddToList;

		public Log(LogKind kind, long associatedId, string serializedData, object data) : this()
		{
			Kind = kind;
			AssociatedId = associatedId;
			Data = serializedData;
			ParsedData = data;
		}

		public void Notify()
		{
			NotificationEvent?.Invoke(this);
		}

		public static Log NewTimePlayedLog(long usergameId, TimeSpan timePlayed, bool notify)
		{
			var log = new Log(LogKind.TimePlayed, usergameId, timePlayed.ToString(), timePlayed);
			if (notify) log.Notify();
			else Debug.WriteLine(log);
			AddToList?.Invoke(log);
			return log;
		}

		public static Log NewStartedPlayingLog(long usergameId, DateTime time)
		{
			var log = new Log(LogKind.StartedPlaying, usergameId, time.ToString(CultureInfo.InvariantCulture), time);
			log.Notify();
			return log;
		}

		public Paragraph GetParagraph()
		{
			var paragraph = new Paragraph();
			switch (Kind)
			{
				case LogKind.TimePlayed:
					var userGame1 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
					paragraph.Inlines.Add("Played ");
					paragraph.Inlines.Add(new Run(userGame1?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame") { Foreground = Brushes.Green });
					paragraph.Inlines.Add($" for {((TimeSpan)ParsedData).ToHumanReadable()}.");
					break;
				case LogKind.StartedPlaying:
					var userGame2 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
					paragraph.Inlines.Add("Started playing ");
					paragraph.Inlines.Add(new Run(userGame2?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame") { Foreground = Brushes.Green });
					paragraph.Inlines.Add($" at {((DateTime)ParsedData):hh\\:mm}");
					break;
				case LogKind.Simple:
					paragraph.Inlines.Add(new Run(Data));
					break;
				default:
					paragraph.Inlines.Add($"[{Kind}] Unknown Kind - ");
					paragraph.Inlines.Add(new Run($"AssociatedId {AssociatedId}") { Foreground = Brushes.Green });
					paragraph.Inlines.Add($" - Data {Data}");
					break;
			}
			return paragraph;
		}


		public static Log NewSimpleLog(string message)
		{
			var log = new Log(LogKind.Simple, 0, message, message);
			log.Notify();
			return log;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			switch (Kind)
			{
				case LogKind.TimePlayed:
					var userGame1 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
					sb.Append("Played ");
					sb.Append(userGame1?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame");
					sb.Append($" for {((TimeSpan)ParsedData).ToHumanReadable()}.");
					break;
				case LogKind.StartedPlaying:
					var userGame2 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
					sb.Append("Started playing ");
					sb.Append(userGame2?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame");
					sb.Append($" at {((DateTime)ParsedData).GetLocalizedTime()}.");
					break;
				case LogKind.Simple:
					sb.Append(new Run(Data));
					break;
				default:
					sb.Append($"[{Kind}] Unknown Kind - ");
					sb.Append($"AssociatedId {AssociatedId}");
					sb.Append($" - Data {Data}");
					break;
			}
			return sb.ToString();
		}
	}

	public enum LogKind { TimePlayed = 0, StartedPlaying = 1, Simple = 2 }
}