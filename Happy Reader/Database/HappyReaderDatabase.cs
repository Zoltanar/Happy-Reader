using System.Data.Entity;

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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

    }
}
