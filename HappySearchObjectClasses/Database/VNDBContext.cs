using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.CodeFirst;
using static Happy_Apps_Core.StaticHelpers;
using System.Linq;
// ReSharper disable VirtualMemberCallInConstructor

namespace Happy_Apps_Core.Database
{
    using System.Data.Entity;

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public partial class VisualNovelDatabase : DbContext
    {
        // Your context has been configured to use a 'Model1' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'Happy_Apps_Core.Database.Model1' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'Model1' 
        // connection string in the application configuration file.
        public VisualNovelDatabase() : base("name=VNDatabase")
        {
            /*LogToFile("Visual Novels = " + VisualNovels.Count());
            LogToFile("Producers = " + Producers.Count());
            LogToFile("Characters = " + Characters.Count());
            LogToFile("User Visual Novels = " + UserVisualNovels.Count());*/
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new VNDatabaseInitializer(modelBuilder));
        }

        public virtual DbSet<ListedVN> VisualNovels { get; set; }
        public virtual DbSet<ListedProducer> Producers { get; set; }
        public virtual DbSet<UserVN> UserVisualNovels { get; set; }
        public virtual DbSet<CharacterItem> Characters { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<TableDetail> TableDetails { get; set; }

        public IQueryable<UserVN> URTVisualNovels => UserVisualNovels.Where(x => x.UserId == Settings.UserID);
    }

    public class VNDatabaseInitializer : SqliteCreateDatabaseIfNotExists<VisualNovelDatabase>
    {
        protected override void Seed(VisualNovelDatabase context)
        {
            context.TableDetails.Add(new TableDetail { Key = "programname", Value = "Happy Search" });
            context.TableDetails.Add(new TableDetail { Key = "author", Value = "Zoltanar" });
            context.TableDetails.Add(new TableDetail { Key = "projecturl", Value = "https://github.com/Zoltanar/Happy-Search" });
            context.TableDetails.Add(new TableDetail { Key = "databaseversion", Value = "2.0.0" });
            base.Seed(context);
        }

        public VNDatabaseInitializer(DbModelBuilder modelBuilder) : base(modelBuilder){}
    }

    public class User
    {
        public User()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            FavoriteProducers = new HashSet<ListedProducer>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Username { get; set; }

        public virtual ICollection<ListedProducer> FavoriteProducers { get; set; }

        public override string ToString() => $"[{Id}] {Username}";
    }

    public class TableDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}