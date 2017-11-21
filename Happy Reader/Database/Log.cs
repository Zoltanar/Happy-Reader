using System;
using System.Linq;

namespace Happy_Reader.Database
{
    public class Log
    {
        public long Id { get; set; }
        public LogKind Kind { get; set; }
        public long AssociatedId { get; set; }
        public string Data { get; set; }

        public Log(LogKind kind, long associatedId, string data)
        {
            Kind = kind;
            AssociatedId = associatedId;
            Data = data;
            Console.WriteLine(GetMessage());
        }

        public static Log NewTimePlayedLog(long usergameId, TimeSpan timePlayed)
        {
            return new Log(LogKind.TimePlayed, usergameId, timePlayed.ToString());
        }
        
        public string GetMessage()
        {
            switch (Kind)
            {
                case LogKind.TimePlayed:
                    var userGame = StaticMethods.Data.UserGames.FirstOrDefault(g => g.Id == AssociatedId);
                    return
                        $"Played {userGame?.DisplayName ?? $"Unknown UserGame ID {AssociatedId}"} for {TimeSpan.Parse(Data):hh\\:mm}";
            }
            return $"Unknown kind {Kind} - AssociatedId {AssociatedId} - Data {Data}";
        }
    }

    public enum LogKind { TimePlayed = 0 }
}