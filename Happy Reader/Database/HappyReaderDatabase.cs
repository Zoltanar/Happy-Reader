using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Happy_Apps_Core.Database;
using IthVnrSharpLib;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class HappyReaderDatabase : DbContext
	{
		public HappyReaderDatabase() : base("name=HappyReaderDatabase") { }

		public virtual DbSet<Entry> Entries { get; set; }
		public virtual DbSet<UserGame> UserGames { get; set; }
		public virtual DbSet<HRGoogleTranslate.GoogleTranslation> CachedTranslations { get; set; }
		public virtual DbSet<Log> Logs { get; set; }
		public virtual DbSet<GameTextThread> GameThreads { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder) { }

		public IQueryable<Entry> GetGameOnlyEntries(ListedVN game) => Entries.Where(x => x.GameId == game.VNID);

		public IQueryable<Entry> GetSeriesOnlyEntries(ListedVN game)
		{
			var series = StaticHelpers.LocalDatabase.VisualNovels.Where(i => i.Series == game.Series).Select(i => i.VNID).ToArray();
			return Entries.Where(i => series.Contains(i.GameId.Value));
		}

		public override int SaveChanges()
		{
			int result = base.SaveChanges();
			var caller = new StackFrame(1).GetMethod();
			var callerName = $"{caller.DeclaringType?.Name}.{caller.Name}";
			StaticHelpers.Logger.ToDebug($"{System.DateTime.Now.ToShortTimeString()} - {nameof(HappyReaderDatabase)}.{nameof(SaveChanges)} called by {callerName} - returned {result}");
			return result;
		}

		public override async Task<int> SaveChangesAsync()
		{
			int result = await base.SaveChangesAsync();
			var caller = new StackFrame(1).GetMethod();
			var callerName = $"{caller.DeclaringType?.Name}.{caller.Name}";
			StaticHelpers.Logger.ToDebug($"{System.DateTime.Now.ToShortTimeString()} - {nameof(HappyReaderDatabase)}.{nameof(SaveChangesAsync)} called by {callerName} - returned {result}");
			return result;
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
		PreRomaji = 12,
		PostRomaji = 15,
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
