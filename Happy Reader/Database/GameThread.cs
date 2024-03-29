﻿using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core;
using IthVnrSharpLib;

namespace Happy_Reader.Database
{
	public class GameThread : Happy_Apps_Core.DataAccess.IDataItem<(long, string)>
	{
		public readonly GameTextThread Item;

		public GameThread()
		{
			Item = new GameTextThread();
		}

		public GameThread(GameTextThread item)
		{
			Item = item;
		}

		public string KeyField { get; } = $"({nameof(GameTextThread.GameId)},{nameof(GameTextThread.Identifier)})";
		public (long, string) Key => (Item.GameId, Item.Identifier);

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(GameThread)}s" +
			             "(GameId, Identifier, IsDisplay, IsPaused, IsPosting, Encoding, Label, RetnRight, Spl) VALUES " +
			             "(@GameId, @Identifier, @IsDisplay, @IsPaused, @IsPosting, @Encoding, @Label, @RetnRight, @Spl)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@GameId", Item.GameId);
			command.AddParameter("@Identifier", Item.Identifier);
			command.AddParameter("@IsDisplay", Item.IsDisplay);
			command.AddParameter("@IsPaused", Item.IsPaused);
			command.AddParameter("@IsPosting", Item.IsPosting);
			command.AddParameter("@Encoding", Item.Encoding);
			command.AddParameter("@Label", Item.Label);
			//these are stored are longs because uint is misinterpreted in sqlite
			command.AddParameter("@RetnRight", (long)Item.RetnRight);
			command.AddParameter("@Spl", (long)Item.Spl);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			Item.GameId = Convert.ToInt32(reader["GameId"]);
			Item.Identifier = Convert.ToString(reader["Identifier"]);
			Item.IsDisplay = Convert.ToInt32(reader["IsDisplay"]) == 1;
			Item.IsPaused = Convert.ToInt32(reader["IsPaused"]) == 1;
			Item.IsPosting = Convert.ToInt32(reader["IsPosting"]) == 1;
			Item.Encoding = Convert.ToString(reader["Encoding"]);
			Item.Label = Convert.ToString(reader["Label"]);
			//these are stored are longs because uint is misinterpreted in sqlite
			Item.RetnRight = (uint)Convert.ToInt64(reader["RetnRight"]);
			Item.Spl = (uint)Convert.ToInt64(reader["Spl"]);
		}
	}
}