﻿using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Translation
{
	public class CachedTranslation : IDataItem<string>
	{
		public CachedTranslation(string input, string output, string source)
		{
			Input = input;
			Output = output;
			CreatedAt = DateTime.UtcNow;
			Timestamp = DateTime.UtcNow;
			Count = 1;
			Source = source;
		}

		public CachedTranslation()
		{

		}

		public string Input { get; private set; }
		public string Output { get; private set; }
		/// <summary>
		/// Timestamp of creation
		/// </summary>
		public DateTime CreatedAt { get; private set; }
		/// <summary>
		/// Timestamp of last used
		/// </summary>
		public DateTime Timestamp { get; private set; }
		/// <summary>
		/// Count of times used
		/// </summary>
		public int Count { get; private set; }
		public string Source { get; private set; }
		public string KeyField => nameof(Input);
		public string Key => Input;

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(CachedTranslation)}s" +
									 "(Input, Output, CreatedAt, Timestamp, Count, Source) VALUES " +
									 "(@Input, @Output, @CreatedAt, @Timestamp, @Count, @Source)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@Input", Input);
			command.AddParameter("@Output", Output);
			command.AddParameter("@CreatedAt", CreatedAt);
			command.AddParameter("@Timestamp", Timestamp);
			command.AddParameter("@Count", Count);
			command.AddParameter("@Source", Source);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			Input = Convert.ToString(reader["Input"]);
			Output = Convert.ToString(reader["Output"]);
			CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
			Timestamp = Convert.ToDateTime(reader["Timestamp"]);
			Count = Convert.ToInt32(reader["Count"]);
			Source = Convert.ToString(reader["Source"]);
		}

		/// <summary>
		/// Increase count by 1 and update timestamp.
		/// </summary>
		public void Update()
		{
			Count++;
			Timestamp = DateTime.UtcNow;
		}
	}
}