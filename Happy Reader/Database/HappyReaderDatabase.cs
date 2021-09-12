using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using Happy_Apps_Core.Translation;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
	public class HappyReaderDatabase
	{
		private readonly object _saveChangesLock = new();
		private const string UpdateTableName = @"Updates";

		public SQLiteConnection Connection { get; }
		public DACollection<string, CachedTranslation> Translations { get; }
		public DACollection<long, Log> Logs { get; }
		public DACollection<(long, string), GameThread> GameThreads { get; }
		public DACollection<long, UserGame> UserGames { get; }
		public DACollection<long, Entry> Entries { get; }

		public HappyReaderDatabase(string dbFile, bool loadAllTables)
		{
			Connection = new SQLiteConnection($@"Data Source={dbFile}");
			Translations = new DACollection<string, CachedTranslation>(Connection);
			Logs = new DACollection<long, Log>(Connection);
			GameThreads = new DACollection<(long, string), GameThread>(Connection);
			UserGames = new DACollection<long, UserGame>(Connection);
			Entries = new DACollection<long, Entry>(Connection);
			if (!File.Exists(dbFile)) CreateDatabase();
			RunUpdates();
			if (!loadAllTables) return;
			LoadAllTables();
		}

		private void RunUpdates()
		{
			try
			{
				Connection.Open();
				var command = Connection.CreateCommand();
				command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{UpdateTableName}';";
				var responseObject = command.ExecuteScalar();
				bool updateTableExists = responseObject != null && responseObject != DBNull.Value;
				if (!updateTableExists)
				{
					DatabaseTableBuilder.ExecuteSql(Connection, $@"CREATE TABLE `{UpdateTableName}` (
	`Id`	INTEGER NOT NULL UNIQUE,
	`Timestamp`	DATETIME,
	PRIMARY KEY(`Id`)
)");
				}

				command.CommandText = $"SELECT MAX(Id) FROM {UpdateTableName};";
				responseObject = command.ExecuteScalar();
				var latestUpdate = responseObject == DBNull.Value ? 0 : Convert.ToInt32(responseObject);
				RunUpdates(latestUpdate);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				//throw;
			}
			finally
			{
				Connection.Close();
			}
		}

		private void RunUpdates(int currentUpdate)
		{
			bool backedUp = false;
			var assembly = Assembly.GetExecutingAssembly();
			do
			{
				currentUpdate++;
				var update = $"Update{currentUpdate:#}";
				var nextUpdateFile = assembly.GetManifestResourceStream($"Happy_Reader.Database.Updates.{update}.sql");
				if (nextUpdateFile == null) return;
				if (!backedUp)
				{
					StaticHelpers.Logger.ToFile("Backing up Happy Reader Database to run updates.");
					var dbFile = new FileInfo(Connection.FileName);
					var backupFile = $"{dbFile.DirectoryName}\\{Path.GetFileNameWithoutExtension(dbFile.FullName)}-UB{DateTime.Now:yyyyMMdd-HHmmss}{dbFile.Extension}";
					dbFile.CopyTo(backupFile);
					backedUp = true;
				}
				StaticHelpers.Logger.ToFile($"Updating Happy Reader Database with {update}");
				using var reader = new StreamReader(nextUpdateFile);
				var contents = reader.ReadToEnd();
				DatabaseTableBuilder.ExecuteSql(Connection, contents);
			} while (true);
		}

		private void LoadAllTables()
		{
			Connection.Open();
			try
			{
				Translations.Load(false);
				Logs.Load(false);
				GameThreads.Load(false);
				UserGames.Load(false);
				Entries.Load(false);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
			finally
			{
				Connection.Close();
			}
		}

		private void CreateDatabase()
		{
			Connection.Open();
			try
			{
				DatabaseTableBuilder.ExecuteSql(Connection, $@"CREATE TABLE `{nameof(CachedTranslation)}s` (
	`Input`	TEXT NOT NULL UNIQUE,
	`Output`	TEXT,
	`CreatedAt`	DATETIME,
	`Timestamp`	DATETIME,
	`Count`	INTEGER,
  `Source` TEXT NOT NULL,
	PRIMARY KEY(`Input`)
)");
				DatabaseTableBuilder.ExecuteSql(Connection, $@"CREATE TABLE `{nameof(Log)}s` (
	`Id`	INTEGER NOT NULL UNIQUE,
	`Kind`	INTEGER,
	`AssociatedId`	INTEGER,
	`Data`	TEXT,
	`Timestamp`	DATETIME,
	PRIMARY KEY(`Id`)
)");
				DatabaseTableBuilder.ExecuteSql(Connection, $@"CREATE TABLE `{nameof(GameThread)}s` (
	`GameId`	INTEGER NOT NULL,
	`Identifier`	TEXT NOT NULL,
	`IsDisplay`	INTEGER,
	`IsPaused`	INTEGER,
	`IsPosting`	INTEGER,
	`Encoding`	TEXT,
	`Label`	TEXT,
	PRIMARY KEY(`GameId`, `Identifier`)
)");
				DatabaseTableBuilder.ExecuteSql(Connection, $@"CREATE TABLE `{nameof(UserGame)}s` (
	`Id`	INTEGER NOT NULL UNIQUE,
	`UserDefinedName`	TEXT,
	`LaunchPath`	TEXT,
	`HookProcess`	INTEGER NOT NULL,
	`VNID`	INTEGER,
	`FilePath`	TEXT,
	`HookCode`	TEXT,
	`MergeByHookCode`	INTEGER,
	`ProcessName`	TEXT,
	`Tag`	TEXT,
	`RemoveRepetition`	INTEGER,
	`OutputWindow`	TEXT,
	`TimeOpenDT`	DATETIME,
	`PrefEncodingEnum`	INTEGER,
	`LaunchModeOverride`	INTEGER NOT NULL,
	PRIMARY KEY(`Id`)
)");
				DatabaseTableBuilder.ExecuteSql(Connection, $@"CREATE TABLE `{nameof(Entry)}s` (
	`Id`	INTEGER NOT NULL UNIQUE,
	`UserId`	INTEGER,
	`Input`	TEXT,
	`Output`	TEXT,
	`GameId`	INTEGER,
	`SeriesSpecific`	INTEGER,
	`Private`	INTEGER,
	`Priority`	REAL,
	`Regex`	INTEGER,
	`Comment`	TEXT,
	`Type`	INTEGER,
	`RoleString`	TEXT,
	`Disabled`	INTEGER,
	`Time`	DATETIME,
	`UpdateTime`	DATETIME,
	`UpdateUserId`	INTEGER,
	`UpdateComment`	TEXT,
	`GameIdIsUserGame` INTEGER NOT NULL,
	PRIMARY KEY(`Id`)
)");
				DatabaseTableBuilder.ExecuteSql(Connection, $@"CREATE TABLE `{UpdateTableName}` (
	`Id`	INTEGER NOT NULL UNIQUE,
	`Timestamp`	DATETIME,
	PRIMARY KEY(`Id`)
)");
				DatabaseTableBuilder.ExecuteSql(Connection, $@"INSERT INTO {UpdateTableName} (Id,Timestamp) VALUES (2,datetime());");
			}
			finally
			{
				Connection.Close();
			}
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

		public static EntryGame[] GetSeriesEntryGames(EntryGame entryGame)
		{
			if (entryGame?.GameId == null) return Array.Empty<EntryGame>();
			var array = new[] {entryGame};
			if (entryGame.IsUserGame) return array;
			var vn = StaticHelpers.LocalDatabase.VisualNovels[entryGame.GameId.Value];
			if (vn == null) return array;
			var gamesInSeries = array.Concat(vn.GetAllRelations().Select(r => new EntryGame(r.ID, false, false))).ToArray();
			return gamesInSeries;
		}

		public IEnumerable<Entry> GetSeriesOnlyEntries(EntryGame game)
		{
			var series = GetSeriesEntryGames(game);
			return Entries.Where(i => i.GameData.GameId.HasValue && series.Contains(i.GameData));
		}

		public int SaveChanges()
		{
			lock (_saveChangesLock)
			{
				var exceptions = new List<Exception>();
				int totalResult = 0;
				totalResult += WrapInExceptionList(exceptions, () => Translations.SaveChanges());
				totalResult += WrapInExceptionList(exceptions, () => Logs.SaveChanges());
				totalResult += WrapInExceptionList(exceptions, () => GameThreads.SaveChanges());
				totalResult += WrapInExceptionList(exceptions, () => UserGames.SaveChanges());
				totalResult += WrapInExceptionList(exceptions, () => Entries.SaveChanges());
				var caller = new StackFrame(1).GetMethod();
				var callerName = $"{caller.DeclaringType?.Name}.{caller.Name}";
				StaticHelpers.Logger.ToDebug(
					$"{DateTime.Now.ToShortTimeString()} - {nameof(HappyReaderDatabase)}.{nameof(SaveChanges)} called by {callerName} - returned {totalResult}");
				if (exceptions.Any()) throw new AggregateException(exceptions);
				return totalResult;
			}
		}

		public void DeleteCachedTranslationsOlderThan(DateTime dateTime)
		{
			var sql = $"DELETE FROM {nameof(CachedTranslation)}s WHERE Timestamp < @Timestamp";
			Connection.Open();
			try
			{
				var cmd = Connection.CreateCommand();
				cmd.CommandText = sql;
				cmd.AddParameter("@Timestamp", dateTime);
				var result = cmd.ExecuteNonQuery();
				StaticHelpers.Logger.ToFile($"Deleted Cached Translations older than {dateTime}: {result} records.");
				if (result == 0) return;
				Translations.Load(false);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			finally
			{
				Connection.Close();
			}
		}
		
		public void DeleteAllCachedTranslations()
		{
			var sql = $"DELETE FROM {nameof(CachedTranslation)}s";
			Connection.Open();
			try
			{
				var cmd = Connection.CreateCommand();
				cmd.CommandText = sql;
				var result = cmd.ExecuteNonQuery();
				StaticHelpers.Logger.ToFile($"Deleted all Cached Translations: {result} records.");
				if (result == 0) return;
				Translations.Load(false);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			finally
			{
				Connection.Close();
			}
		}

		/// <summary>
		/// Upserts entries into database, inserting Ids as required, opens own connection and transaction.
		/// </summary>
		public void AddEntries(ICollection<Entry> entries)
		{
			if (entries.Count == 0) return;
			Connection.Open();
			DbTransaction transaction = null;
			try
			{
				transaction = Connection.BeginTransaction();
				foreach (var entry in entries)
				{
					if (entry.Id == 0) entry.Id = Entries.HighestKey + 1;
					Entries.Upsert(entry, false, false, transaction);
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
				Connection.Close();
			}
		}
	}
}
