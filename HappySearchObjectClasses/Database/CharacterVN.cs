using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	public sealed class CharacterVN : IDataItem<(int, int)>, IDumpItem
	{
		public int CharacterId { get; set; }
		public int RId { get; set; }
		public int Spoiler { get; set; }
		public string Role { get; set; }
		public int VNId { get; set; }

		public static CharacterVN From(CharacterItem.VNItem cvn,int characterId)
		{
			var result = new CharacterVN
			{
				CharacterId = characterId,
				VNId = cvn.ID,
				RId = cvn.RID,
				Spoiler = cvn.Spoiler,
				Role = cvn.Role
			};
			return result;
		}

		public override string ToString() => $"[CID: {CharacterId}, VNID: {VNId}]";

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public void LoadFromStringParts(string[] parts)
		{
			CharacterId = Convert.ToInt32(parts[0]);
			VNId = Convert.ToInt32(parts[1]);
			RId = 0;//Convert.ToInt32(parts[2]); //todo ??
			Spoiler = Convert.ToInt32(parts[3]);
			Role = Convert.ToString(parts[4]);
		}

		#region IDataItem Implementation

		public string KeyField => "(CharacterId,VNID)";
		public (int, int) Key => (CharacterId, VNId);
		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO CharacterVNs" +
									 "(CharacterId,VNID,RId,Spoiler,Role) VALUES " +
									 "(@CharacterId,@VNID,@RId,@Spoiler,@Role)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@CharacterId", CharacterId);
			command.AddParameter("@VNID", VNId);
			command.AddParameter("@RId", RId);
			command.AddParameter("@Spoiler", Spoiler);
			command.AddParameter("@Role", Role);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			CharacterId = Convert.ToInt32(reader["CharacterId"]);
			VNId = Convert.ToInt32(reader["VNID"]);
			RId = Convert.ToInt32(reader["RId"]);
			Spoiler = Convert.ToInt32(reader["Spoiler"]);
			Role = Convert.ToString(reader["Role"]);
		}
		#endregion
	}
}