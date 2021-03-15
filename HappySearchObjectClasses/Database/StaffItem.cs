using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	public class StaffItem : IDataItem<int>, IDumpItem
	{
		public int ID { get; set; }
		public int AliasID { get; set; }
		public string Gender { get; set; }
		public string Language { get; set; }
		public string Description { get; set; }

		public string KeyField => nameof(ID);
		public int Key => ID;
		public static Dictionary<string, int> Headers { get; set; }

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(StaffItem)}s " +
									 "(ID,AID,Gender,Lang,Desc) VALUES " +
									 "(@ID,@AID,@Gender,@Lang,@Desc)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@ID", ID);
			command.AddParameter("@AID", AliasID);
			command.AddParameter("@Gender", Gender);
			command.AddParameter("@Lang", Language);
			command.AddParameter("@Desc", Description);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			try
			{
				ID = Convert.ToInt32(reader["ID"]);
				AliasID = Convert.ToInt32(reader["AID"]);
				Gender = Convert.ToString(reader["Gender"]);
				Language = Convert.ToString(reader["Lang"]);
				Description = Convert.ToString(reader["Desc"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}

		public void LoadFromStringParts(string[] parts)
		{
			ID = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			AliasID = Convert.ToInt32(GetPart(parts, "aid"));
			Gender = GetPart(parts, "gender");
			Language = GetPart(parts, "lang");
			Description = GetPart(parts, "desc");
		}

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];
	}
}
