using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using HRGoogleTranslate;
using IthVnrSharpLib;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class HappyReaderDatabase : DbContext
	{
		#region SQLite
		private SQLiteConnection SqliteConnection { get; }

		public DACollection<string, GoogleTranslation> SqliteTranslations { get; private set; }

		public HappyReaderDatabase(string dbFile) : base("name=HappyReaderDatabase")
		{
			SqliteConnection = new SQLiteConnection($@"Data Source={dbFile}");
			InitialiseSqliteDatabase(dbFile, true);
		}

		private void InitialiseSqliteDatabase(string dbFile, bool loadAllTables)
		{
			SqliteTranslations = new DACollection<string, GoogleTranslation>(SqliteConnection);
			if (!File.Exists(dbFile)) SeedSqlite();
			if (!loadAllTables) return;
			LoadAllSqliteTables();
		}

		private void LoadAllSqliteTables()
		{
			SqliteConnection.Open();
			try
			{
				SqliteTranslations.Load(false);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
			finally
			{
				SqliteConnection.Close();
			}
		}

		private void SeedSqlite()
		{
			SqliteConnection.Open();
			try
			{
				DatabaseTableBuilder.ExecuteSql(SqliteConnection, $@"CREATE TABLE ""{nameof(GoogleTranslation)}"" (
	`Input`	TEXT NOT NULL UNIQUE,
	`Output`	TEXT,
	`CreatedAt`	DATETIME,
	`Timestamp`	DATETIME,
	`Count`	INTEGER,
	PRIMARY KEY(`Input`)
)");
			}
			finally
			{
				SqliteConnection.Close();
			}
		}
		#endregion

		public virtual DbSet<Entry> Entries { get; set; }
		public virtual DbSet<UserGame> UserGames { get; set; }
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
			var exceptions = new List<Exception>();
			int totalResult = 0;
			try { SqliteTranslations.SaveChanges(); }
			catch (Exception ex) { exceptions.Add(ex); }
			try { totalResult += base.SaveChanges(); }
			catch (Exception ex) { exceptions.Add(ex); }
			var caller = new StackFrame(1).GetMethod();
			var callerName = $"{caller.DeclaringType?.Name}.{caller.Name}";
			StaticHelpers.Logger.ToDebug($"{DateTime.Now.ToShortTimeString()} - {nameof(HappyReaderDatabase)}.{nameof(SaveChanges)} called by {callerName} - returned {totalResult}");
			if (exceptions.Any()) throw new AggregateException(exceptions);
			return totalResult;
		}

		public override async Task<int> SaveChangesAsync()
		{
			int result = await base.SaveChangesAsync();
			var caller = new StackFrame(1).GetMethod();
			var callerName = $"{caller.DeclaringType?.Name}.{caller.Name}";
			StaticHelpers.Logger.ToDebug($"{DateTime.Now.ToShortTimeString()} - {nameof(HappyReaderDatabase)}.{nameof(SaveChangesAsync)} called by {callerName} - returned {result}");
			return result;
		}

		public void DeleteCachedTranslationsOlderThan(DateTime dateTime)
		{
			var sql = $"DELETE FROM {nameof(GoogleTranslation)}s WHERE Timestamp < @Timestamp";
			SqliteConnection.Open();
			try
			{
				var cmd = SqliteConnection.CreateCommand();
				cmd.CommandText = sql;
				cmd.AddParameter("@Timestamp", dateTime);
				var result = cmd.ExecuteNonQuery();
				StaticHelpers.Logger.ToFile($"Deleted Cached Translations older than {dateTime}: {result} records.");
				if (result == 0) return;
				SqliteTranslations.Load(false);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			finally
			{
				SqliteConnection.Close();
			}
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
