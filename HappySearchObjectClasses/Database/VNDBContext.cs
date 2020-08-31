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
	public class VisualNovelDatabase
	{
		public DACollection<int, ListedVN> VisualNovels { get; }
		public DACollection<int, ListedProducer> Producers { get; }
		public DACollection<(int, int), UserVN> UserVisualNovels { get; }
		public DACollection<(int, int), UserListedProducer> UserProducers { get; }
		public DACollection<int, CharacterItem> Characters { get; }
		public DAListCollection<int, (int, int), CharacterVN> CharacterVNs { get; }
		public DACollection<int, User> Users { get; }
		public DACollection<string, TableDetail> TableDetails { get; }
		public DAListCollection<int, (int, int), DbTag> Tags { get; }
		public DAListCollection<int, (int, int), DbTrait> Traits { get; }
		public DACollection<int, StaffItem> StaffItems { get; }
		public DACollection<int, StaffAlias> StaffAliases { get; }
		public DACollection<(int, int, string), VnStaff> VnStaffs { get; }
		public DACollection<(int, int, int), VnSeiyuu> VnSeiyuus { get; }

		public VisualNovelDatabase(string dbFile, bool loadAllTables)
		{
			Connection = new SQLiteConnection($@"Data Source={dbFile}");
			VisualNovels = new DACollection<int, ListedVN>(Connection);
			UserVisualNovels = new DACollection<(int, int), UserVN>(Connection);
			Producers = new DACollection<int, ListedProducer>(Connection);
			UserProducers = new DACollection<(int, int), UserListedProducer>(Connection);
			Users = new DACollection<int, User>(Connection);
			TableDetails = new DACollection<string, TableDetail>(Connection);
			CharacterVNs = new DAListCollection<int, (int, int), CharacterVN>(Connection);
			Characters = new DACollection<int, CharacterItem>(Connection);
			Traits = new DAListCollection<int, (int, int), DbTrait>(Connection);
			Tags = new DAListCollection<int, (int, int), DbTag>(Connection);
			StaffItems = new DACollection<int, StaffItem>(Connection);
			StaffAliases = new DACollection<int, StaffAlias>(Connection);
			VnStaffs = new DACollection<(int, int, string), VnStaff>(Connection);
			VnSeiyuus = new DACollection<(int, int, int), VnSeiyuu>(Connection);
			if (!File.Exists(dbFile)) Seed();
			if (!loadAllTables) return;
			LoadAllTables();
		}

		private void LoadAllTables()
		{
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
				Traits.Load(false);
				Tags.Load(false);
				StaffItems.Load(false);
				StaffAliases.Load(false);
				VnStaffs.Load(false);
				VnSeiyuus.Load(false);
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
					var vnid = Convert.ToInt32(vnidObject);
					character.CharacterVN = CharacterVNs.ByKey(vnid, (character.ID, vnid));
				}
			}
			finally
			{
				Connection.Close();
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

		public double GetTraitScoreForVn(int vnid, Dictionary<int, double> idTraits, bool newConnection)
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

		public static Func<ListedVN, bool> SearchForVN(string searchString)
		{
			var lowerSearchString = searchString.ToLower();
			if (searchString.StartsWith("\"") && searchString.EndsWith("\""))
			{
				var trimmedSearchString = lowerSearchString.Trim('\"');
				return vn => HasWholeWord(vn.Title, trimmedSearchString) 
				             || HasWholeWord(vn.KanjiTitle, trimmedSearchString) 
				             || HasWholeWord(vn.Aliases, trimmedSearchString);
			}
			return vn => vn.Title.ToLower().Contains(lowerSearchString) ||
			             vn.KanjiTitle != null && vn.KanjiTitle.ToLower().Contains(lowerSearchString) ||
			             vn.Aliases != null && vn.Aliases.ToLower().Contains(lowerSearchString);
		}

		public static Func<CharacterItem, bool> SearchForCharacter(string searchString)
		{
			var lowerSearchString = searchString.ToLower();
			if (searchString.StartsWith("\"") && searchString.EndsWith("\""))
			{
				var trimmedSearchString = lowerSearchString.Trim('\"');
				return ch => HasWholeWord(ch.Name, trimmedSearchString) 
				             || HasWholeWord(ch.Original, trimmedSearchString) 
				             || HasWholeWord(ch.Aliases, trimmedSearchString);
			}
			return ch => ch.Name.ToLower().Contains(lowerSearchString) ||
			             ch.Original != null && ch.Original.ToLower().Contains(lowerSearchString) ||
			             ch.Aliases != null && ch.Aliases.ToLower().Contains(lowerSearchString);
		}

		private static bool HasWholeWord(string input, string searchString)
		{
			return !string.IsNullOrWhiteSpace(input) && input.ToLower().Split(' ').Any(p => p == searchString);
		}

		public void DeleteForDump()
		{
			Connection.Open();
			try
			{
				var trans = Connection.BeginTransaction();
				DeleteTable("CharacterItems", trans);
				DeleteTable("CharacterVNs", trans);
				DeleteTable("DbTags", trans);
				DeleteTable("DbTraits", trans);
				DeleteTable("ListedProducers", trans);
				DeleteTable("ListedVNs", trans);
				DeleteTable("UserVNs", trans);
				DeleteTable("StaffItems", trans);
				DeleteTable("StaffAliass", trans);
				DeleteTable("VnStaffs", trans);
				DeleteTable("VnSeiyuus", trans);
				trans.Commit();
			}
			finally
			{
				Connection.Close();
			}
		}

		private void DeleteTable(string tableName, SQLiteTransaction trans)
		{
			var command = Connection.CreateCommand();
			command.CommandText = $@"DELETE FROM {tableName}";
			command.Transaction = trans;
			command.ExecuteNonQuery();
			command.Dispose();
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