using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	/// <summary>
	/// Key is (CharacterItemId, TraitId)
	/// </summary>
	public sealed class DbTrait : IDataItem<(int, int)>, IDumpItem
	{

		public int CharacterItem_Id { get; set; }

		public int TraitId { get; set; }

		public int Spoiler { get; set; }

		public static DbTrait From(CharacterItem.TraitItem trait, int cid)
		{
			var result = new DbTrait
			{
				CharacterItem_Id = cid,
				TraitId = trait.ID,
				Spoiler = trait.Spoiler
			};
			return result;
		}


		#region IDataItem Implementation

		public string KeyField { get; } = "(CharacterItem_Id, TraitId)";
		public (int, int) Key => (CharacterItem_Id, TraitId);

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $@"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO DbTraits (TraitId,Spoiler,CharacterItem_Id) VALUES (@TraitId,@Spoiler,@CharacterItem_Id)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@TraitId", TraitId);
			command.AddParameter("@Spoiler", Spoiler);
			command.AddParameter("@CharacterItem_Id", CharacterItem_Id);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			if (reader == null) throw new ArgumentNullException(nameof(reader));
			try
			{
				CharacterItem_Id = Convert.ToInt32(reader["CharacterItem_Id"]);
				TraitId = Convert.ToInt32(reader["TraitId"]);
				Spoiler = Convert.ToInt32(reader["Spoiler"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}
		#endregion
		
		public static EqualityComparer ValueComparer { get; } = new EqualityComparer();

		public class EqualityComparer : IEqualityComparer<DbTrait>
		{
			public bool Equals(DbTrait x, DbTrait y)
			{
				if (x is null && y is null) return true;
				if (x == null ^ y == null) return false;
				if (x.CharacterItem_Id != y.CharacterItem_Id) return false;
				if (x.TraitId != y.TraitId) return false;
				return true;
			}

			public int GetHashCode(DbTrait obj)
			{
				unchecked
				{
					return (obj.CharacterItem_Id * 397) ^ obj.TraitId;
				}
			}
		}

		public void LoadFromStringParts(string[] parts)
		{
			CharacterItem_Id = Convert.ToInt32(GetPart(parts,"id"));
			TraitId = Convert.ToInt32(GetPart(parts, "tid"));
			Spoiler = Convert.ToInt32(GetPart(parts, "spoil"));
		}

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}
	}
}