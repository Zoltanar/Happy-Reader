using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	public class StaffAlias : DumpItem,  IDataItem<int>
	{
		public int StaffID { get; set; }
		public int AliasID { get; set; }
		public string Name { get; set; }
		public string Original { get; set; }

		public string KeyField => nameof(AliasID);
		public int Key => AliasID;

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(StaffAlias)}s " +
									 "(StaffID,AliasID,Name,Original) VALUES " +
									 "(@StaffID,@AliasID,@Name,@Original)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@StaffID", StaffID);
			command.AddParameter("@AliasID", AliasID);
			command.AddParameter("@Name", Name);
			command.AddParameter("@Original", Original);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			try
			{
				StaffID = Convert.ToInt32(reader["StaffID"]);
				AliasID = Convert.ToInt32(reader["AliasID"]);
				Name = Convert.ToString(reader["Name"]);
				Original = Convert.ToString(reader["Original"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}

		public override void LoadFromStringParts(string[] parts)
		{
			StaffID = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			AliasID = Convert.ToInt32(GetPart(parts, "aid"));
			Name = GetPart(parts, "latin");
			Original = GetPart(parts, "name");
		}
		
		public override string ToString()
		{
			var original = string.IsNullOrWhiteSpace(Original) ? "" : $" ({Original})";
			return $"{Name}{original}";
		}
	}
}
