using System.Data.SQLite;

namespace Happy_Apps_Core.Database
{
	internal static class DatabaseTableBuilder
	{
		internal static void CreateTables(SQLiteConnection connection)
		{
			CreateCharacterItems(connection);
			CreateCharacterVns(connection);
			CreateDbTags(connection);
			CreateDbTraits(connection);
			CreateListedProducers(connection);
			CreateListedVNs(connection);
			CreateTableDetails(connection);
		}

		private static void CreateDbTraits(SQLiteConnection connection)
		{
			var command = connection.CreateCommand();
			var sql = @"CREATE TABLE ""DbTraits"" (
	""TraitId""	INTEGER NOT NULL,
	""Spoiler""	INTEGER,
	""CharacterItem_ID""	INTEGER NOT NULL,
	PRIMARY KEY(""CharacterItem_ID"",""TraitId"")
)";
			command.CommandText = sql;
			command.ExecuteNonQuery();
			command.Dispose();
		}

		private static void CreateCharacterVns(SQLiteConnection connection)
		{
			var command = connection.CreateCommand();
			var sql = @"CREATE TABLE ""CharacterVNs"" (
	""CharacterId""	INTEGER,
	""VNID""	INTEGER,
	""RId""	INTEGER,
	""Spoiler""	INTEGER,
	""Role""	TEXT,
	PRIMARY KEY(""CharacterId"",""VNID"")
)";
			command.CommandText = sql;
			command.ExecuteNonQuery();
			command.Dispose();
		}

		private static void CreateCharacterItems(SQLiteConnection connection)
		{
			var command = connection.CreateCommand();
			var sql = @"CREATE TABLE ""CharacterItems"" (
	""ID""	INTEGER NOT NULL UNIQUE,
	""Name""	TEXT,
	""Original""	TEXT,
	""Gender""	TEXT,
	""BloodT""	INTEGER,
	""BirthDate""	DATE,
	""VNs""	TEXT,
	""DateUpdated""	DATE,
	""Aliases""	TEXT,
	""Description""	TEXT,
	""Image""	INTEGER,
	PRIMARY KEY(""ID"")
)";
			command.CommandText = sql;
			command.ExecuteNonQuery();
			command.Dispose();
		}

		private static void CreateDbTags(SQLiteConnection connection)
		{
			var command = connection.CreateCommand();
			var sql = @"CREATE TABLE ""DbTags"" (
	""ListedVN_VNID""	INTEGER NOT NULL,
	""TagId""	INTEGER NOT NULL,
	""Score""	REAL,
	""Spoiler""	INTEGER,
	""Category""	INTEGER,
	PRIMARY KEY(""ListedVN_VNID"",""TagId"")
)";
			command.CommandText = sql;
			command.ExecuteNonQuery();
			command.Dispose();
		}

		private static void CreateListedProducers(SQLiteConnection connection)
		{
			var command = connection.CreateCommand();
			var sql = @"CREATE TABLE ""ListedProducers"" (
	""ProducerID""	INTEGER NOT NULL,
	""Name""	TEXT,
	""Language""	TEXT,
	PRIMARY KEY(""ProducerID"")
)";
			command.CommandText = sql;
			command.ExecuteNonQuery();
			command.Dispose();
		}

		private static void CreateTableDetails(SQLiteConnection connection)
		{
			var command = connection.CreateCommand();
			var sql = @"CREATE TABLE `tabledetails` (
	`Key`	TEXT NOT NULL,
	`Value`	TEXT,
	PRIMARY KEY(`Key`)
)";
			command.CommandText = sql;
			command.ExecuteNonQuery();
			command.Dispose();
		}

		private static void CreateListedVNs(SQLiteConnection connection)
		{
			var command = connection.CreateCommand();
			var sql = @"CREATE TABLE ""ListedVNs"" (
	""VNID""	INTEGER NOT NULL UNIQUE,
	""Title""	TEXT,
	""KanjiTitle""	TEXT,
	""ReleaseDateString""	TEXT,
	""ProducerID""	INTEGER,
	""DateUpdated""	DATE DEFAULT CURRENT_TIMESTAMP,
	""Image""	INTEGER,
	""ImageNSFW""	INTEGER,
	""Description""	TEXT,
	""LengthTime""	INTEGER,
	""Popularity""	NUMERIC,
	""Rating""	NUMERIC,
	""VoteCount""	INTEGER,
	""Relations""	TEXT,
	""Screens""	TEXT,
	""Anime""	TEXT,
	""Aliases""	TEXT,
	""Languages""	TEXT,
	""DateFullyUpdated""	DATE,
	""Series""	TEXT,
	""ReleaseDate""	DATE,
	PRIMARY KEY(""VNID"")
)";
			command.CommandText = sql;
			command.ExecuteNonQuery();
			command.Dispose();
		}
	}
}