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
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class HappyReaderDatabase : DbContext
	{
		#region SQLite
		private SQLiteConnection SqliteConnection { get; }

		public DACollection<string, GoogleTranslation> SqliteTranslations { get; private set; }
		public DACollection<long, Log> SqliteLogs { get; private set; }
		public DACollection<(long,string), GameThread> SqliteGameThreads { get; private set; }

		public HappyReaderDatabase(string dbFile) : base("name=HappyReaderDatabase")
		{
			SqliteConnection = new SQLiteConnection($@"Data Source={dbFile}");
			InitialiseSqliteDatabase(dbFile, true);
		}

		private void InitialiseSqliteDatabase(string dbFile, bool loadAllTables)
		{
			SqliteTranslations = new DACollection<string, GoogleTranslation>(SqliteConnection);
			SqliteLogs = new DACollection<long, Log>(SqliteConnection);
			SqliteGameThreads = new DACollection<(long, string), GameThread>(SqliteConnection);
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
				SqliteLogs.Load(false);
				SqliteGameThreads.Load(false);
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
				DatabaseTableBuilder.ExecuteSql(SqliteConnection, $@"CREATE TABLE `{nameof(GoogleTranslation)}s` (
	`Input`	TEXT NOT NULL UNIQUE,
	`Output`	TEXT,
	`CreatedAt`	DATETIME,
	`Timestamp`	DATETIME,
	`Count`	INTEGER,
	PRIMARY KEY(`Input`)
)");
				DatabaseTableBuilder.ExecuteSql(SqliteConnection, $@"CREATE TABLE `{nameof(Log)}s` (
	`Id`	INTEGER NOT NULL UNIQUE,
	`Kind`	INTEGER,
	`AssociatedId`	INTEGER,
	`Data`	TEXT,
	`Timestamp`	DATETIME,
	PRIMARY KEY(`Id`)
)");
				DatabaseTableBuilder.ExecuteSql(SqliteConnection, $@"CREATE TABLE `{nameof(GameThread)}s` (
	`GameId`	INTEGER NOT NULL,
	`Identifier`	TEXT NOT NULL,
	`IsDisplay`	INTEGER,
	`IsPaused`	INTEGER,
	`IsPosting`	INTEGER,
	`Encoding`	TEXT,
	`Label`	TEXT,
	PRIMARY KEY(`GameId`, `Identifier`)
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
			totalResult += WrapInExceptionList(exceptions, () => SqliteTranslations.SaveChanges());
			totalResult += WrapInExceptionList(exceptions, () => SqliteLogs.SaveChanges());
			totalResult += WrapInExceptionList(exceptions, () => SqliteGameThreads.SaveChanges());
			totalResult += WrapInExceptionList(exceptions, () => base.SaveChanges());
			var caller = new StackFrame(1).GetMethod();
			var callerName = $"{caller.DeclaringType?.Name}.{caller.Name}";
			StaticHelpers.Logger.ToDebug($"{DateTime.Now.ToShortTimeString()} - {nameof(HappyReaderDatabase)}.{nameof(SaveChanges)} called by {callerName} - returned {totalResult}");
			if (exceptions.Any()) throw new AggregateException(exceptions);
			return totalResult;
		}

		private static T WrapInExceptionList<T>(ICollection<Exception> exceptionsCollection, Func<T> action)
		{
			try
			{
				return action();
			}
			catch (Exception ex)
			{
				exceptionsCollection.Add(ex);
			}
			return default;
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

		public void SaveToSqlite()
		{
			/*
			if (SqliteGameThreads.Count != 0) return;
			SqliteConnection.Open();
			SQLiteTransaction transaction = null;
			try
			{
				transaction = SqliteConnection.BeginTransaction();
				int dupes = 0;
				foreach (var itemGroup in GameThreads.AsEnumerable().GroupBy(a=>(a.GameId,a.Identifier)))
				{
					var a = itemGroup.Count();
					if (a > 1)
					{
						dupes++;
					}
					SqliteGameThreads.Add(new GameThread(itemGroup.First()), false, true, transaction);
				}
				transaction.Commit();
			}
			catch
			{
				transaction?.Rollback();
				throw;
			}
			finally
			{
				SqliteConnection.Close();
			}*/
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
