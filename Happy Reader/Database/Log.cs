using System;
using System.Globalization;
using System.Linq;

namespace Happy_Reader.Database
{
    public class Log
    {
        public long Id { get; set; }
        public LogKind Kind { get; set; }
        public long AssociatedId { get; set; }
        public string Data { get; set; }

        public static event StaticMethods.NotificationEventHandler NotificationEvent;

        public Log(LogKind kind, long associatedId, string data)
        {
            Kind = kind;
            AssociatedId = associatedId;
            Data = data;
        }

        public void Notify()
        {
            NotificationEvent?.Invoke(this, GetMessage(),Kind.ToString());
        }

        public static Log NewTimePlayedLog(long usergameId, TimeSpan timePlayed)
        {
            var log = new Log(LogKind.TimePlayed, usergameId, timePlayed.ToString());
            log.Notify();
            return log;
        }

        public static Log NewStartedPlayingLog(long usergameId, DateTime time)
        {
            var log = new Log(LogKind.StartedPlaying, usergameId, time.ToString(CultureInfo.InvariantCulture));
            log.Notify();
            return log;
        }

        public string GetMessage()
        {
            switch (Kind)
            {
                case LogKind.TimePlayed:
                    var userGame1 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
                    return $"Played {userGame1?.DisplayName ?? $"Unknown UserGame ID {AssociatedId}"} for {TimeSpan.Parse(Data):hh\\:mm}";
                case LogKind.StartedPlaying:
                    var userGame2 = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
                    return $"Started playing {userGame2?.DisplayName ?? $"Unknown UserGame ID {AssociatedId}"} at {DateTime.Parse(Data,CultureInfo.InvariantCulture):hh\\:mm}";

            }
            return $"Unknown Kind {Kind} - AssociatedId {AssociatedId} - Data {Data}";
        }

        public static Log NewSimpleLog(string title, string message)
        {
            var log = new Log(LogKind.Simple, 0, message);
            log.Notify();
            return log;
        }
    }

    public enum LogKind { TimePlayed = 0, StartedPlaying = 1, Simple = 2 }
}