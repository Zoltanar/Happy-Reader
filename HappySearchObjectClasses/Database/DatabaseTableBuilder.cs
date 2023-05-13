using System.Data.SQLite;

namespace Happy_Apps_Core.Database
{
	public static class DatabaseTableBuilder
	{
		internal static void CreateHappyAppsTables(SQLiteConnection connection)
		{
			CreateCharacterItems(connection);
			CreateCharacterVns(connection);
			CreateDbTags(connection);
			CreateDbTraits(connection);
			CreateListedProducers(connection);
			CreateListedVNs(connection);
			CreateTableDetails(connection);
			CreateStaffTables(connection);
			CreateUserTables(connection);
		}


		private static void CreateUserTables(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE ""Users"" (
	`Id`	INTEGER NOT NULL UNIQUE,
	`Username`	TEXT,
	PRIMARY KEY(`Id`)
)");

			ExecuteSql(connection, @"CREATE TABLE ""UserVNs"" (
	""VNID""	INTEGER,
	""UserID""	INTEGER,
	""ULNote""	TEXT,
	""Vote""	INTEGER,
	""VoteAdded""	DATETIME,
	""Added""	DATETIME,
	""Labels""	TEXT,
	""LastModified""	DATETIME,
	PRIMARY KEY(""UserID"",""VNID"")
)");

			ExecuteSql(connection, @"CREATE TABLE ""UserListedProducers"" (
	`ListedProducer_Id`	INTEGER NOT NULL,
	`User_Id`	INTEGER NOT NULL,
	`UserAverageVote`	NUMERIC,
	`UserDropRate`	INTEGER,
	PRIMARY KEY(`ListedProducer_Id`,`User_Id`)
)");
		}


		private static void CreateStaffTables(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE ""StaffItems"" (
	""ID""	INTEGER,
	""AID""	INTEGER,
	""Gender""	TEXT,
	""Lang""	TEXT,
	""Desc""	TEXT,
	PRIMARY KEY(""ID"")
)");
			ExecuteSql(connection, @"CREATE TABLE ""StaffAliass"" (
	""StaffID""	INTEGER,
	""AliasID""	INTEGER,
	""Name""	TEXT,
	""Original""	TEXT,
	PRIMARY KEY(""StaffID"",""AliasID"")
)");
			ExecuteSql(connection, @"CREATE TABLE ""VnStaffs"" (
	""VNID""	INTEGER,
	""AID""	INTEGER,
	""Role""	TEXT,
	""Note""	TEXT,
	PRIMARY KEY(""VNID"",""AID"",""Role"")
)");
			ExecuteSql(connection, @"CREATE TABLE ""VnSeiyuus"" (
	""VNID""	INTEGER,
	""AID""	INTEGER,
	""CID""	INTEGER,
	""Note""	TEXT,
	PRIMARY KEY(""VNID"",""AID"",""CID"")
)");
		}

		private static void CreateDbTraits(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE ""DbTraits"" (
	""TraitId""	INTEGER NOT NULL,
	""Spoiler""	INTEGER,
	""CharacterItem_ID""	INTEGER NOT NULL,
	PRIMARY KEY(""CharacterItem_ID"",""TraitId"")
)");
		}

		private static void CreateCharacterVns(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE ""CharacterVNs"" (
	""CharacterId""	INTEGER,
	""VNID""	INTEGER,
	""RId""	INTEGER,
	""Spoiler""	INTEGER,
	""Role""	TEXT,
	PRIMARY KEY(""CharacterId"",""VNID"")
)");
		}

		private static void CreateCharacterItems(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE ""CharacterItems"" (
	""ID""	INTEGER NOT NULL UNIQUE,
	""Name""	TEXT,
	""Original""	TEXT,
	""Gender""	TEXT,
	""Aliases""	TEXT,
	""Description""	TEXT,
	""Image""	TEXT,
	""TraitScore""	REAL,
	PRIMARY KEY(""ID"")
);");
		}

		private static void CreateDbTags(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE ""DbTags"" (
	""ListedVN_VNID""	INTEGER NOT NULL,
	""TagId""	INTEGER NOT NULL,
	""Score""	REAL,
	""Spoiler""	INTEGER,
	""Category""	INTEGER,
	PRIMARY KEY(""ListedVN_VNID"",""TagId"")
)");
		}

		private static void CreateListedProducers(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE ""ListedProducers"" (
	""ProducerID""	INTEGER NOT NULL,
	""Name""	TEXT,
	""Language""	TEXT,
	PRIMARY KEY(""ProducerID"")
)");
		}

		public static void CreateTableDetails(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE `tabledetails` (
	`Key`	TEXT NOT NULL,
	`Value`	TEXT,
	PRIMARY KEY(`Key`)
)");
		}

		private static void CreateListedVNs(SQLiteConnection connection)
		{
			ExecuteSql(connection, @"CREATE TABLE ""ListedVNs"" (
	""VNID""	INTEGER NOT NULL UNIQUE,
	""Title""	TEXT,
	""KanjiTitle""	TEXT,
	""ReleaseDateString""	TEXT,
	""ProducerID""	INTEGER,
	""Image""	TEXT,
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
	""ReleaseDate""	DATE,
	""ReleaseLink""	TEXT,
	""TagScore""	REAL,
	""TraitScore""	REAL,
	""NewSinceUpdate""	INTEGER,
	PRIMARY KEY(""VNID"")
);");
		}

		public static void ExecuteSql(SQLiteConnection connection, string sql)
		{
			using var command = connection.CreateCommand();
			command.CommandText = sql;
			command.ExecuteNonQuery();
		}
	}
}