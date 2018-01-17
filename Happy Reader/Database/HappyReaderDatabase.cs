using System.Data.Entity;
using System.Linq;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader.Database
{


    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class HappyReaderDatabase : DbContext
    {
        public HappyReaderDatabase()
            : base("name=HappyReaderDatabase")
        {
        }

        public virtual DbSet<Entry> Entries { get; set; }
        public virtual DbSet<UserGame> UserGames { get; set; }
        public virtual DbSet<HRGoogleTranslate.Translation> CachedTranslations { get; set; }
        public virtual DbSet<Log> Logs { get; set; }
        public string[] UserGameProcesses => UserGames.Where(x => x != null).Select(x => x.ProcessName).ToArray();

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        public IQueryable<Entry> GetGameOnlyEntries(ListedVN game) => Entries.Where(x=>x.GameId == game.VNID);

        public IQueryable<Entry> GetSeriesOnlyEntries(ListedVN game)
        {
            var series = StaticHelpers.LocalDatabase.VisualNovels.Where(i => i.Series == game.Series).Select(i => i.VNID).ToArray();
            return Entries.Where(i => series.Contains( i.GameId.Value));
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
