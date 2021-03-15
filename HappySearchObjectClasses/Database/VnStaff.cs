using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	public class VnStaff : IDataItem<(int, int, string)>, IDumpItem
	{
		public int VNID { get; set; }
		public int AliasID { get; set; }
		public string Role { get; set; }
		public string Note { get; set; }

		public string KeyField => "(VNID,AliasID)";
		public (int, int, string) Key => (VNID, AliasID, Role);
		public static Dictionary<string, int> Headers { get; set; }

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(VnStaff)}s " +
									 "(VNID,AID,Role,Note) VALUES " +
									 "(@VNID,@AID,@Role,@Note)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@VNID", VNID);
			command.AddParameter("@AID", AliasID);
			command.AddParameter("@Role", Role);
			command.AddParameter("@Note", Note);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			try
			{
				VNID = Convert.ToInt32(reader["VNID"]);
				AliasID = Convert.ToInt32(reader["AID"]);
				Role = Convert.ToString(reader["Role"]);
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
			Role = GetPart(parts, "role");
			Note = GetPart(parts, "note");
		}

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public string Detail
		{
			get
			{
				var alias = StaticHelpers.LocalDatabase.StaffAliases[AliasID];
				var original = string.IsNullOrWhiteSpace(alias.Original) ? "" : $" ({alias.Original})";
				var note = string.IsNullOrWhiteSpace(Note) ? "" : $" - {Note}";
				return $"{alias.Name}{original}{note}";
			}
		}

		public override string ToString()
		{
			var alias = StaticHelpers.LocalDatabase.StaffAliases[AliasID];
			var original = string.IsNullOrWhiteSpace(alias.Original) ? "" : $" ({alias.Original})";
			return $"{alias.Name}{original} - {RoleDetail} - {Note}";
		}

		public string RoleDetail
		{
			get
			{
				return Role switch
				{
					"empty" => "Empty",
					"art" => "Art",
					"chardesign" => "Character Design",
					"scenario" => "Scenario",
					"music" => "Music",
					"director" => "Director",
					"staff" => "Staff",
					"songs" => "Vocals",
					_ => Role
				};
			}
		}
	}
}
