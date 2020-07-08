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
		public int AID { get; set; }
		public string Gender { get; set; }
		public string Lang { get; set; }
		public string Description { get; set; }
		/*
		public string WP { get; set; }
		public string Site { get; set; }
		public string Twitter { get; set; }
		public int AniDB { get; set; }
		public int Wikidata { get; set; }
		public int Pixiv { get; set; }
		*/
		public string KeyField => nameof(ID);
		public int Key => ID;
		public Dictionary<string, int> Headers { get; set; }
		
		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(StaffItem)}s" +
									 "(ID,AID,Gender,Lang,Description,) VALUES " +
									 "(@ID,@AID,@Gender,@Lang,@Desc)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@ID", ID);
			command.AddParameter("@AID", AID);
			command.AddParameter("@Gender", Gender);
			command.AddParameter("@Lang", Lang);
			command.AddParameter("@Description", Description);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			try
			{
				ID = Convert.ToInt32(reader["ID"]);
				AID = Convert.ToInt32(reader["AID"]);
				Gender = Convert.ToString(reader["Gender"]);
				Lang = Convert.ToString(reader["Lang"]);
				Description = Convert.ToString(reader["Description"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}

		public static StaffItem FromReader(IDataRecord reader)
		{
			var item = new StaffItem();
			item.LoadFromReader(reader);
			return item;
		}

		public void LoadFromStringParts(string[] parts)
		{
			ID = Convert.ToInt32(GetPart(parts, "id"));
			AID = Convert.ToInt32(GetPart(parts, "aid"));
			Gender = GetPart(parts, "gender");
			Lang = GetPart(parts, "lang");
			Description = GetPart(parts, "desc");
		}

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}
		
		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];
	}
}
