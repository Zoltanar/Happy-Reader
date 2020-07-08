using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using Happy_Apps_Core.DataAccess;

// ReSharper disable VirtualMemberCallInConstructor

namespace Happy_Apps_Core.Database
{
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public partial class VisualNovelDatabase
	{
		public DACollection<int, ListedVN> VisualNovels { get; }
		public DACollection<int, ListedProducer> Producers { get; }
		public DACollection<(int, int), UserVN> UserVisualNovels { get; }
		public DACollection<(int, int), UserListedProducer> UserProducers { get; }
		public DACollection<int, CharacterItem> Characters { get; set; }
		public DACollection<(int, int), CharacterVN> CharacterVNs { get; set; }
		public DACollection<int, CharacterStaff> CharacterStaffs { get; set; }
		public DACollection<int, User> Users { get; set; }
		public DACollection<string, TableDetail> TableDetails { get; set; }
		public DACollection<(int, int), DbTag> Tags { get; set; }
		public DACollection<(int, int), DbTrait> Traits { get; set; }

		public VisualNovelDatabase(bool loadAllTables) : this(StaticHelpers.DatabaseFile, loadAllTables)
		{ }

		public VisualNovelDatabase(string dbFile, bool loadAllTables)
		{
			Connection = new SQLiteConnection($@"Data Source={dbFile}");
			VisualNovels = new DACollection<int, ListedVN>(Connection);
			UserVisualNovels = new DACollection<(int, int), UserVN>(Connection);
			Producers = new DACollection<int, ListedProducer>(Connection);
			UserProducers = new DACollection<(int, int), UserListedProducer>(Connection);
			Users = new DACollection<int, User>(Connection);
			TableDetails = new DACollection<string, TableDetail>(Connection);
			CharacterVNs = new DACollection<(int, int), CharacterVN>(Connection);
			Characters = new DACollection<int, CharacterItem>(Connection);
			CharacterStaffs = new DACollection<int, CharacterStaff>(Connection);
			Traits = new DACollection<(int, int), DbTrait>(Connection);
			Tags = new DACollection<(int, int), DbTag>(Connection);
			if (!File.Exists(dbFile)) Seed();
			if (!loadAllTables) return;
			Connection.Open();
			try
			{
				VisualNovels.Load(false);
				UserVisualNovels.Load(false);
				Producers.Load(false);
				UserProducers.Load(false);
				Users.Load(false);
				TableDetails.Load(false);
				CharacterVNs.Load(false);
				Characters.Load(false);
				CharacterStaffs.Load(false);
				Traits.Load(false);
				Tags.Load(false);
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

		public SQLiteConnection Connection { get; }

		public IEnumerable<ListedVN> URTVisualNovels => VisualNovels.Where(x => x.UserVN != null);
		public User CurrentUser { get; set; }

		public int ExecuteSqlCommand(string query, bool openNewConnection)
		{
			if (openNewConnection) Connection.Open();
			try
			{
				using var command = Connection.CreateCommand();
				command.CommandText = query;
				var result = command.ExecuteNonQuery();
				return result;
			}
			finally
			{
				if (openNewConnection) Connection.Close();
			}
		}

		public List<CharacterItem> GetCharactersForVn(int vnid)
		{
			var items = new List<CharacterItem>();
			Connection.Open();
			try
			{
				using var command = Connection.CreateCommand();
				command.CommandText =
					@"select CharacterItems.* from CharacterItems join CharacterVNs on CharacterItems.ID = CharacterVNs.CharacterId where VNID = @vnid;";
				command.AddParameter("@vnid", vnid);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var item = new CharacterItem();
					item.LoadFromReader(reader);
					items.Add(item);
				}
				return items;
			}
			finally
			{
				Connection.Close();
			}
		}

		public void SetCharactersAttachedVisualNovels()
		{
			Connection.Open();
			try
			{
				using var command = Connection.CreateCommand();
				command.CommandText = @"select ID,ListedVNs.ReleaseDate, ListedVNs.VNID, ListedVNs.ProducerID, CharacterItems.Image
from CharacterItems
left join CharacterVNs on CharacterItems.ID = CharacterVNs.CharacterId
left join ListedVNs on CharacterVNs.VNID = ListedVNs.VNID 
left join DbTraits on CharacterItems.ID = DbTraits.CharacterItem_ID
GROUP BY (ID)  
order by ListedVNs.ReleaseDate desc;";
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var character = Characters[Convert.ToInt32(reader["ID"])];
					var vnidObject = reader["VNID"];
					if (vnidObject == DBNull.Value) continue;
					character.CharacterVN = CharacterVNs[(character.ID, Convert.ToInt32(vnidObject))];
				}
			}
			finally
			{
				Connection.Close();
			}
		}

		public List<DbTag> GetTagsForVn(int vnid)
		{
			lock (Connection)
			{
				var items = new List<DbTag>();
				var newConnection = Connection.State == ConnectionState.Closed;
				if (newConnection) Connection.Open();
				try
				{
					using var command = Connection.CreateCommand();
					command.CommandText =
						@"select DbTags.* from DbTags where ListedVN_VNID = @vnid;";
					command.AddParameter("@vnid", vnid);
					using var reader = command.ExecuteReader();
					while (reader.Read())
					{
						var item = new DbTag();
						item.LoadFromReader(reader);
						items.Add(item);
					}

					return items;
				}
				finally
				{
					if (newConnection) Connection.Close();
				}
			}
		}

		public List<DbTrait> GetCharactersTraitsForVn(int vnid, bool newConnection)
		{
			var items = new List<DbTrait>();
			if (newConnection)
			{
				Monitor.Enter(Connection);
				Connection.Open();
			}
			try
			{
				using var command = Connection.CreateCommand();
				command.CommandText =
					@"select DbTraits.* from DbTraits join CharacterItems on CharacterItems.ID = DbTraits.CharacterItem_ID join CharacterVNs on CharacterItems.ID = CharacterVNs.CharacterId where VNID = @vnid;";
				command.AddParameter("@vnid", vnid);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var item = new DbTrait();
					item.LoadFromReader(reader);
					items.Add(item);
				}

				return items;
			}
			finally
			{
				if (newConnection)
				{
					Connection.Close();
					Monitor.Exit(Connection);
				}
			}
		}
		
		public double GetTraitScoreForVn(int vnid,Dictionary<int, double> idTraits, bool newConnection)
		{
			var score = 0d;
			if (newConnection)
			{
				Monitor.Enter(Connection);
				Connection.Open();
			}
			try
			{
				using var command = Connection.CreateCommand();
				command.CommandText =
					$@"select DbTraits.TraitId from DbTraits 
join CharacterItems on CharacterItems.ID = DbTraits.CharacterItem_ID 
join CharacterVNs on CharacterItems.ID = CharacterVNs.CharacterId 
where VNID = @vnid;";
				command.AddParameter("@vnid", vnid);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					if (idTraits.TryGetValue(Convert.ToInt32(reader["TraitId"]), out var value)) score += value;
				}
				return score;
			}
			finally
			{
				if (newConnection)
				{
					Connection.Close();
					Monitor.Exit(Connection);
				}
			}
		}

		public IEnumerable<CharacterItem> GetCharactersWithTrait(int traitID)
		{
			Connection.Open();
			try
			{
				using var command = Connection.CreateCommand();
				command.CommandText = @"select ID
from CharacterItems
left join DbTraits on CharacterItems.ID = DbTraits.CharacterItem_ID
where TraitId = @TraitId";
				command.AddParameter("@TraitId", traitID);
				var list = new List<CharacterItem>();
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					list.Add(Characters[Convert.ToInt32(reader["ID"])]);
				}
				return list;
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
				DatabaseTableBuilder.CreateTables(Connection);
				TableDetails.Upsert(new TableDetail { Key = "programname", Value = "Happy Reader" }, false);
				TableDetails.Upsert(new TableDetail { Key = "author", Value = "Zoltanar" }, false);
				TableDetails.Upsert(new TableDetail { Key = "projecturl", Value = StaticHelpers.ProjectURL }, false);
				TableDetails.Upsert(new TableDetail { Key = "databaseversion", Value = "2.0.0" }, false);
				//ExecuteSqlCommand("CREATE UNIQUE INDEX `UniqueCIDToAliasToVN` ON `CharacterStaffs` (`AliasId` ,`ListedVNId` ,`CharacterItem_Id` )", false);
				//ExecuteSqlCommand("CREATE UNIQUE INDEX `UniqueCIDToTrait` ON `DbTraits` (`TraitId` ,`CharacterItem_ID` )", false);
				//ExecuteSqlCommand("CREATE UNIQUE INDEX `UniqueCIDToVNID` ON `CharacterVNs` (`ListedVNId` ,`CharacterItem_Id` )", false);
				//ExecuteSqlCommand("CREATE UNIQUE INDEX `UniqueTagIdToVNID` ON `DbTags` (`ListedVN_VNID` ,`TagId` )", false);
			}
			finally
			{
				Connection.Close();
			}
		}
	}

	public class TableDetail : IDataItem<string>
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public string Key { get; set; }

		public string Value { get; set; }

		#region IDataItem implementation

		public string KeyField { get; } = "Key";

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(TableDetail)}s" +
									 "(Key,Value) VALUES " +
									 "(@Key,@Value)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@Key", Key);
			command.AddParameter("@Value", Value);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			Key = Convert.ToString(reader["Key"]);
			Value = Convert.ToString(reader["Value"]);
		}
		#endregion

	}
}