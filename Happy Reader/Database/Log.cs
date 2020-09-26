using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace Happy_Reader.Database
{
	public class Log : ICloneable
	{
		private object _parsedData;
		private List<Inline> _inlines;

		public Log() { }

		public long Id { get; set; }
		public LogKind Kind { get; set; }
		public long AssociatedId { get; set; }
		public string Data { get; set; }

		[NotMapped]
		public List<Inline> Description => _inlines ??= GetInlines();

		[NotMapped]
		public object ParsedData
		{
			get
			{
				if (_parsedData != null) return _parsedData;
				switch (Kind)
				{
					case LogKind.ResetTimePlayed:
						return null;
					case LogKind.MergeTimePlayed:
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

		private Log(LogKind kind, long associatedId, string serializedData, object data)
		{
			Timestamp = DateTime.UtcNow;
			Kind = kind;
			AssociatedId = associatedId;
			Data = serializedData;
			ParsedData = data;
		}

		public void Notify()
		{
			NotificationEvent?.Invoke(this);
		}

		public static Log NewTimePlayedLog(long userGameId, TimeSpan timePlayed, bool notify)
		{
			var log = new Log(LogKind.TimePlayed, userGameId, timePlayed.ToString(), timePlayed);
			if (notify) log.Notify();
			else Debug.WriteLine(log);
			AddToList?.Invoke(log);
			return log;
		}

		public static Log NewMergedTimePlayedLog(long userGameId, TimeSpan mergedTimePlayed, bool notify)
		{
			var log = new Log(LogKind.MergeTimePlayed, userGameId, mergedTimePlayed.ToString(), mergedTimePlayed);
			if (notify) log.Notify();
			else Debug.WriteLine(log);
			AddToList?.Invoke(log);
			return log;
		}

		public static Log NewResetTimePlayedLog(long userGameId, bool notify)
		{
			var log = new Log(LogKind.ResetTimePlayed, userGameId, null, null);
			if (notify) log.Notify();
			else Debug.WriteLine(log);
			AddToList?.Invoke(log);
			return log;
		}

		public static Log NewStartedPlayingLog(long userGameId, DateTime time)
		{
			var log = new Log(LogKind.StartedPlaying, userGameId, time.ToString(CultureInfo.InvariantCulture), time);
			log.Notify();
			return log;
		}

		public List<Inline> GetInlines()
		{
			var inlines = new List<Inline>();
			switch (Kind)
			{
				case LogKind.TimePlayed:
					var userGame1 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
					inlines.Add(new Run("Played "));
					inlines.Add(new Run(userGame1?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame") { Foreground = Brushes.Green });
					inlines.Add(new Run($" for {((TimeSpan)ParsedData).ToHumanReadable()}."));
					break;
				case LogKind.StartedPlaying:
					var userGame2 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
					inlines.Add(new Run("Started playing "));
					inlines.Add(new Run(userGame2?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame") { Foreground = Brushes.Green });
					inlines.Add(new Run($" at {((DateTime)ParsedData):hh\\:mm}"));
					break;
				case LogKind.MergeTimePlayed:
					var userGame3 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
					inlines.Add(new Run("Merged time played to "));
					inlines.Add(new Run(userGame3?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame") { Foreground = Brushes.Green });
					inlines.Add(new Run($" for {((TimeSpan)ParsedData).ToHumanReadable()}."));
					break;
				case LogKind.ResetTimePlayed:
					var userGame4 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
					inlines.Add(new Run("Reset time played for "));
					inlines.Add(new Run(userGame4?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame") { Foreground = Brushes.Green });
					break;
				case LogKind.Simple:
					inlines.Add(new Run(Data));
					break;
				default:
					inlines.Add(new Run($"[{Kind}] Unknown Kind - "));
					inlines.Add(new Run($"AssociatedId {AssociatedId}") { Foreground = Brushes.Green });
					inlines.Add(new Run($" - Data {Data}"));
					break;
			}
			return inlines;
		}

		public bool AssociatedIdExists
		{
			get
			{
				switch (Kind)
				{
					case LogKind.TimePlayed:
					case LogKind.StartedPlaying:
					case LogKind.MergeTimePlayed:
					case LogKind.ResetTimePlayed:
						return StaticMethods.Data.UserGames.Any(g => g.Id == AssociatedId);
					default:
						return true;
				}
			}
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
					sb.Append($"Played {GetGameDisplayName()}");
					sb.Append($" for {((TimeSpan)ParsedData).ToHumanReadable()}.");
					break;
				case LogKind.StartedPlaying:
					sb.Append($"Started playing {GetGameDisplayName()}");
					sb.Append($" at {((DateTime)ParsedData).GetLocalizedTime()}.");
					break;
				case LogKind.MergeTimePlayed:
					sb.Append($"Merged time played to {GetGameDisplayName()}");
					sb.Append($" for {((TimeSpan)ParsedData).ToHumanReadable()}.");
					break;
				case LogKind.ResetTimePlayed:
					sb.Append($"Reset Time Played for {GetGameDisplayName()}.");
					break;
				case LogKind.Simple:
					sb.Append(Data);
					break;
				default:
					sb.Append($"[{Kind}] Unknown Kind - ");
					sb.Append($"AssociatedId {AssociatedId}");
					sb.Append($" - Data {Data}");
					break;
			}
			return sb.ToString();
		}

		private string GetGameDisplayName()
		{
			var userGame1 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
			return userGame1?.DisplayName ?? $"[{AssociatedId}] Unknown UserGame";
		}

		public object Clone()
		{
			var newLog = new Log
			{
				ParsedData = this.ParsedData,
				AssociatedId = this.AssociatedId,
				Data = this.Data,
				Kind = this.Kind,
				Timestamp = this.Timestamp
			};
			return newLog;
		}

		public DateTime GetGroupDate(DateTime now)
		{
			var daysAgo = now - Timestamp;
			if (!(daysAgo.TotalDays >= 14)) return Timestamp.Date;
			var week = Math.Ceiling(Timestamp.DayOfYear / 7d);
			var date = new DateTime(Timestamp.Year, 1, 1).AddDays(week * 7 - 1);
			return date;
		}
	}

	public enum LogKind
	{
		[Description("Time Played")]
		TimePlayed = 0,
		[Description("Started Played")]
		StartedPlaying = 1,
		Simple = 2,
		[Description("Merged Time Played")]
		MergeTimePlayed = 3,
		[Description("Reset Time Played")]
		ResetTimePlayed = 4,
	}
}