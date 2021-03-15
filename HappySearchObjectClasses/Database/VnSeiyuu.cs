using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	public class VnSeiyuu : IDataItem<(int, int, int)>, IDumpItem
	{
		public int VNID { get; set; }
		public int AliasID { get; set; }
		public int CharacterID { get; set; }
		public string Note { get; set; }

		public string KeyField => "(VNID, AliasID, CharacterID)";
		public (int, int, int) Key => (VNID, AliasID, CharacterID);
		public static Dictionary<string, int> Headers { get; set; }

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(VnSeiyuu)}s " +
									 "(VNID,AID,CID,Note) VALUES " +
									 "(@VNID,@AID,@CID,@Note)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@VNID", VNID);
			command.AddParameter("@AID", AliasID);
			command.AddParameter("@CID", CharacterID);
			command.AddParameter("@Note", Note);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			try
			{
				VNID = Convert.ToInt32(reader["VNID"]);
				AliasID = Convert.ToInt32(reader["AID"]);
				CharacterID = Convert.ToInt32(reader["CID"]);
				Note = Convert.ToString(reader["Note"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}

		public void LoadFromStringParts(string[] parts)
		{
			VNID = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			AliasID = Convert.ToInt32(GetPart(parts, "aid"));
			CharacterID = Convert.ToInt32(GetPart(parts, "cid").Substring(1));
			Note = GetPart(parts, "note");
		}

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public override string ToString()
		{
			var alias = StaticHelpers.LocalDatabase.StaffAliases[AliasID];
			var original = string.IsNullOrWhiteSpace(alias.Original) ? "" : $" ({alias.Original})";
			return $"{alias.Name}{original}";
		}
	}
}
