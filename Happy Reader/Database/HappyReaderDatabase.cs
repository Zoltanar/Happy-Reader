using System.Data.Entity;
using HRGoogleTranslate;

namespace Happy_Reader.Database
{
    

    public partial class HappyReaderDatabase : DbContext
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
        public virtual DbSet<GameHook> GameHooks { get; set; }
        public virtual DbSet<Translation> CachedTranslations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

    }
}
