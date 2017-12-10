using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using HRGoogleTranslate;

namespace Happy_Reader.Database
{


    public class HappyReaderDatabase : DbContext
    {
        public HappyReaderDatabase()
            : base("name=HappyReaderDatabase")
        {
        }

        public virtual DbSet<Entry> Entries { get; set; }
        public virtual DbSet<Game> Games { get; set; }
        public virtual DbSet<UserGame> UserGames { get; set; }
        public virtual DbSet<GameFile> GameFiles { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Translation> CachedTranslations { get; set; }
        public virtual DbSet<Log> Logs { get; set; }
        public string[] UserGameProcesses => UserGames.Where(x => x != null).Select(x => x.ProcessName).ToArray();

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }


        /// <summary>
        /// Tries to get user by username, returns null if not found.
        /// </summary>
        public User GetUser(string userName) => Users.FirstOrDefault(i => i.Username == userName);

        /// <summary>
        /// Tries to get game by title first, if not found then by romajiTitle, returns null if not found.
        /// </summary>
        public Game GetGameByName(string name) => Games.FirstOrDefault(i => i.Title == name) ?? Games.FirstOrDefault(i => i.RomajiTitle == name);

        public IEnumerable<dynamic> GetGameOnlyEntries(Game game)
        {
            var entryItems = from i in Entries
                             where i.GameId == game.Id
                             from user in Users
                             where i.UserId == user.Id
                             from gameItem in Games
                             where i.GameId == gameItem.Id
                             select new
                             {
                                 i.Id,
                                 User = user.Id,
                                 i.Type,
                                 Game = gameItem.RomajiTitle ?? gameItem.Title,
                                 Role = i.RoleString,
                                 i.Input,
                                 i.Output,
                                 i.SeriesSpecific,
                                 i.Private
                             };
            return entryItems;
        }

        public IEnumerable<dynamic> GetSeriesOnlyEntries(Game game)
        {
            var series = Games.Where(i => i.Series == game.Series).Select(i => i.Id).ToList();
            var entryItems = from i in Entries
                             where series.Contains(i.GameId.Value)
                             from user in Users
                             where i.UserId == user.Id
                             from gameItem in Games
                             where i.GameId == gameItem.Id
                             select new
                             {
                                 i.Id,
                                 User = user.Id,
                                 i.Type,
                                 Game = gameItem.RomajiTitle ?? gameItem.Title,
                                 Role = i.RoleString,
                                 i.Input,
                                 i.Output,
                                 i.SeriesSpecific,
                                 i.Private
                             };
            return entryItems;
        }

        public IQueryable<dynamic> GetAllEntries()
        {
            var entryItems = from i in Entries
                from user in Users
                where i.UserId == user.Id
                from game in Games
                where i.GameId == game.Id
                select new
                {
                    i.Id,
                    User = user.Id,
                    i.Type,
                    Game = game.RomajiTitle ?? game.Title,
                    Role = i.RoleString,
                    i.Input,
                    i.Output,
                    i.SeriesSpecific,
                    i.Private
                };
            return entryItems;
        }
    }

    public enum EntryType
    {
        // ReSharper disable All
        Proxy = -40,
        Macro = -30,
        OCR = -20,
        TTS = -10,
        //stage zero
        Game = 0,
        //stage 1
        Input = 10,
        Yomi = 20,
        //stage 2
        Translation = 30,
        Trans = 30,
        Name = 40,
        ProxyMod = 41,
        Prefix = 45,
        Suffix = 46,
        //stage 3
        Output = 50,
        // ReSharper restore All
    }
}
