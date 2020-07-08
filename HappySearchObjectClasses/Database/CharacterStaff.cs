using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public sealed class CharacterStaff : IDataItem<int>
	{
		public int Id { get; set; }
		public int StaffId { get; set; }
		public int AliasId { get; set; }
		public int ListedVNId { get; set; }
		public string Note { get; set; }
		public int CharacterItem_Id { get; set; }

		public static CharacterStaff From(CharacterItem.StaffItem cStaff, int cid)
		{
			var result = new CharacterStaff
			{
				StaffId = cStaff.ID,
				AliasId = cStaff.AID,
				ListedVNId = cStaff.VID,
				Note = cStaff.Note,
				CharacterItem_Id = cid
			};
			return result;
		}
		
		public string KeyField { get; } = "Id";
		public int Key => Id;
		public static IEqualityComparer<CharacterStaff> KeyComparer { get; } = new EqualityComparer();

		private class EqualityComparer : IEqualityComparer<CharacterStaff>
		{
			public bool Equals(CharacterStaff x, CharacterStaff y)
			{
				if (x is null && y is null) return true;
				if (x == null ^ y == null) return false;
				if (x.StaffId != y.StaffId) return false;
				if (x.ListedVNId != y.ListedVNId) return false;
				if (x.CharacterItem_Id != y.CharacterItem_Id) return false;
				return true;
			}

			public int GetHashCode(CharacterStaff obj)
			{
				unchecked
				{
					var hashCode = obj.StaffId;
					hashCode = (hashCode * 397) ^ obj.ListedVNId;
					hashCode = (hashCode * 397) ^ obj.CharacterItem_Id;
					return hashCode;
				}
			}
		}

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO CharacterStaffs" +
									 "(Id,StaffId,AliasId,ListedVNId,Note,CharacterItem_Id) VALUES " +
									 "(@Id,@StaffId,@AliasId,@ListedVNId,@Note,@CharacterItem_Id)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@Id", Id);
			command.AddParameter("@StaffId", StaffId);
			command.AddParameter("@AliasId", AliasId);
			command.AddParameter("@ListedVNId", ListedVNId);
			command.AddParameter("@Note", Note);
			command.AddParameter("@CharacterItem_Id", CharacterItem_Id);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			Id = Convert.ToInt32(reader["Id"]);
			StaffId = Convert.ToInt32(reader["StaffId"]);
			AliasId = Convert.ToInt32(reader["AliasId"]);
			ListedVNId = Convert.ToInt32(reader["ListedVNId"]);
			Note = Convert.ToString(reader["Note"]);
			CharacterItem_Id = Convert.ToInt32(reader["CharacterItem_Id"]);
		}
	}
}
