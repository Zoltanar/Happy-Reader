using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
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
		public SQLiteConnection Connection { get; }

		[NotMapped]
		public class CachedTranslation : GoogleTranslation, IDataItem<long>
		{
			public string KeyField { get; } = nameof(Id);
			public long Key => Id;
			public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
			{
				string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(CachedTranslation)}s" +
										 "(Id, Input, Output, CreatedAt, Timestamp, Count) VALUES " +
										 "(@Id, @Input, @Output, @CreatedAt, @Timestamp, @Count)";
				var command = connection.CreateCommand();
				command.CommandText = sql;
				command.AddParameter("@Id", Id);
				command.AddParameter("@Input", Input);
				command.AddParameter("@Output", Output);
				command.AddParameter("@CreatedAt", CreatedAt);
				command.AddParameter("@Timestamp", Timestamp);
				command.AddParameter("@Count", Count);
				return command;
			}

			public void LoadFromReader(IDataRecord reader)
			{
				Id = Convert.ToInt64(reader["Id"]);
				Input = Convert.ToString(reader["Input"]);
				Output = Convert.ToString(reader["Output"]);
				CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
				Timestamp = Convert.ToDateTime(reader["Timestamp"]);
				Count = Convert.ToInt32(reader["Count"]);
			}
		}
		public DACollection<long, CachedTranslation> SqliteTranslations { get; private set; }

		public HappyReaderDatabase(string dbFile) : base("name=HappyReaderDatabase")
		{
			Connection = new SQLiteConnection($@"Data Source={dbFile}");
			InitialiseSqliteDatabase(dbFile, true);
		}

		private void InitialiseSqliteDatabase(string dbFile, bool loadAllTables)
		{
			SqliteTranslations = new DACollection<long, CachedTranslation>(Connection);
			if (!File.Exists(dbFile)) Seed();
			if (!loadAllTables) return;
			LoadAllTables();
		}

		private void LoadAllTables()
		{
			Connection.Open();
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
				Connection.Close();
			}
		}

		private void Seed()
		{
			Connection.Open();
			try
			{
				DatabaseTableBuilder.ExecuteSql(Connection, @"CREATE TABLE ""CachedTranslations"" (
	`Id`	INTEGER NOT NULL UNIQUE,
	`Input`	TEXT,
	`Output`	TEXT,
	`CreatedAt`	DATETIME,
	`Timestamp`	DATETIME,
	`Count`	INTEGER,
	PRIMARY KEY(`Id`)
)");
			}
			finally
			{
				Connection.Close();
			}
		}

		public void SaveTranslationsToSqlite()
		{
			if (SqliteTranslations.Count != 0) return;
			Connection.Open();
			SQLiteTransaction transaction = null;
			try
			{
				transaction = Connection.BeginTransaction();
				/*var count = CachedTranslations.Count();
				int skip = 0;
				while (skip < count)
				{*/
				foreach (var translation in CachedTranslations/*.Skip(skip).Take(100)*/)
				{
					var cTranslation = new CachedTranslation()
					{
						Id = translation.Id,
						Input = translation.Input,
						Output = translation.Output,
						CreatedAt = translation.CreatedAt,
						Timestamp = translation.Timestamp,
						Count = translation.Count,
					};
					SqliteTranslations.Add(cTranslation, false, true, transaction);
				}/*
					skip += 100;
				}*/
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
		#endregion

		public virtual DbSet<Entry> Entries { get; set; }
		public virtual DbSet<UserGame> UserGames { get; set; }
		public virtual DbSet<GoogleTranslation> CachedTranslations { get; set; }
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
			StaticHelpers.Logger.ToDebug($"{DateTime.Now.ToShortTimeString()} - {nameof(HappyReaderDatabase)}.{nameof(SaveChanges)} called by {callerName} - returned {result}");
			return result;
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
			Database.Connection.Open();
			try
			{
				var cmd = Database.Connection.CreateCommand();
				cmd.CommandText = sql;
				cmd.AddParameter("@Timestamp", dateTime);
				var result = cmd.ExecuteNonQuery();
				StaticHelpers.Logger.ToFile($"Deleted Cached Translations older than {dateTime}: {result} records.");
				if (result == 0) return;
				CachedTranslations.Load();
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			finally
			{
				Database.Connection.Close();
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
