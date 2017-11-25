using System.Collections.Generic;

namespace Happy_Apps_Core.Database
{
    using System.Data.Entity;

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class VNDatabaseContext : DbContext
    {
        // Your context has been configured to use a 'Model1' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'Happy_Apps_Core.Database.Model1' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'Model1' 
        // connection string in the application configuration file.
        public VNDatabaseContext() : base("name=VNDatabase")
        { }

        public virtual DbSet<ListedVN> VisualNovels { get; set; }
        public virtual DbSet<ListedProducer> Producers { get; set; }
        public virtual DbSet<UserVN> UserVisualNovels { get; set; }
        public virtual DbSet<CharacterItem> Characters { get; set; }
        public virtual DbSet<User> Users { get; set; }
    }

    public class User
    {
        public User()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            FavoriteProducers = new HashSet<ListedProducer>();
        }

        public int Id { get; set; }

        public virtual ICollection<ListedProducer> FavoriteProducers { get; set; }
    }
}