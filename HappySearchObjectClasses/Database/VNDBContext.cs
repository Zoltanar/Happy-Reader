using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.CodeFirst;
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
	    public VisualNovelDatabase(bool loadAllTables) : base("name=VNDatabase")
	    {
		    if (!loadAllTables) return;
			VisualNovels.Load();
			Producers.Load();
			UserVisualNovels.Load();
			CharacterStaffs.Load();
			Users.Load();
			TableDetails.Load();
		    LocalVisualNovels = VisualNovels.Local;
		    LocalProducers = Producers.Local;
		    LocalUserVisualNovels = UserVisualNovels.Local;
		    LocalCharacterStaffs = CharacterStaffs.Local;
		    LocalUsers = Users.Local;
		    LocalTableDetails = TableDetails.Local;
			//these are too slow to load
		    //CharacterVNs.Load();
			//Characters.Load();
			//Tags.Load();
			//Traits.Load();
			//LocalCharacterVNs = CharacterVNs.Local;
			//LocalCharacters = Characters.Local;
			//LocalTags = Tags.Local;
			//LocalTraits = Traits.Local;
		}

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new VNDatabaseInitializer(modelBuilder));
        }

        public virtual DbSet<ListedVN> VisualNovels { get; set; }
        public virtual DbSet<ListedProducer> Producers { get; set; }
        public virtual DbSet<UserVN> UserVisualNovels { get; set; }
        public virtual DbSet<CharacterItem> Characters { get; set; }
        public virtual DbSet<CharacterVN> CharacterVNs { get; set; }
        public virtual DbSet<CharacterStaff> CharacterStaffs { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<TableDetail> TableDetails { get; set; }
        public virtual DbSet<DbTag> Tags { get; set; }
		public virtual DbSet<DbTrait> Traits { get; set; }

	    public readonly ObservableCollection<ListedVN> LocalVisualNovels;
	    public readonly ObservableCollection<ListedProducer> LocalProducers;
	    public readonly ObservableCollection<UserVN> LocalUserVisualNovels;
		public readonly ObservableCollection<CharacterStaff> LocalCharacterStaffs;
	    public readonly ObservableCollection<User> LocalUsers;
	    public readonly ObservableCollection<TableDetail> LocalTableDetails;
		//too slow
	    //public readonly ObservableCollection<CharacterVN> LocalCharacterVNs;
		//public readonly ObservableCollection<CharacterItem> LocalCharacters;
		//public readonly ObservableCollection<DbTag> LocalTags;
		//public readonly ObservableCollection<DbTrait> LocalTraits;

		public IEnumerable<ListedVN> URTVisualNovels => LocalVisualNovels.Where(x => x.UserVNId != null);
        public User CurrentUser { get; set; }
    }

    public class VNDatabaseInitializer : SqliteCreateDatabaseIfNotExists<VisualNovelDatabase>
    {
        protected override void Seed(VisualNovelDatabase context)
        {
            context.TableDetails.Add(new TableDetail { Key = "programname", Value = "Happy Reader" });
            context.TableDetails.Add(new TableDetail { Key = "author", Value = "Zoltanar" });
            context.TableDetails.Add(new TableDetail { Key = "projecturl", Value = "https://github.com/Zoltanar/Happy-Reader" });
            context.TableDetails.Add(new TableDetail { Key = "databaseversion", Value = "2.0.0" });
            context.Database.ExecuteSqlCommand("CREATE UNIQUE INDEX `UniqueCIDToAliasToVN` ON `CharacterStaffs` (`AliasId` ,`ListedVNId` ,`CharacterItem_Id` )");
            context.Database.ExecuteSqlCommand("CREATE UNIQUE INDEX `UniqueCIDToTrait` ON `DbTraits` (`TraitId` ,`CharacterItem_ID` )");
            context.Database.ExecuteSqlCommand("CREATE UNIQUE INDEX `UniqueCIDToVNID` ON `CharacterVNs` (`ListedVNId` ,`CharacterItem_Id` )");
            context.Database.ExecuteSqlCommand("CREATE UNIQUE INDEX `UniqueTagIdToVNID` ON `DbTags` (`ListedVN_VNID` ,`TagId` )");
            base.Seed(context);
        }

        public VNDatabaseInitializer(DbModelBuilder modelBuilder) : base(modelBuilder){}
    }

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
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