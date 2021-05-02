using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;

namespace HRGoogleTranslate
{
	public class GoogleTranslation : IDataItem<string>
	{
		public GoogleTranslation(string input, string output)
		{
			Input = input;
			Output = output;
			CreatedAt = DateTime.UtcNow;
			Timestamp = DateTime.UtcNow;
			Count = 1;
		}

		public GoogleTranslation()
		{
		}
		
		public string Input { get; set; }
		public string Output { get; private set; }
		/// <summary>
		/// Timestamp of creation
		/// </summary>
		public DateTime CreatedAt { get; set; }
		/// <summary>
		/// Timestamp of last used
		/// </summary>
		public DateTime Timestamp { get; private set; }
		/// <summary>
		/// Count of times used
		/// </summary>
		public int Count { get; private set; }
		public string KeyField => nameof(Input);
		public string Key => Input;

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(GoogleTranslation)}s" +
			             "(Input, Output, CreatedAt, Timestamp, Count) VALUES " +
			             "(@Input, @Output, @CreatedAt, @Timestamp, @Count)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@Input", Input);
			command.AddParameter("@Output", Output);
			command.AddParameter("@CreatedAt", CreatedAt);
			command.AddParameter("@Timestamp", Timestamp);
			command.AddParameter("@Count", Count);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			Input = Convert.ToString(reader["Input"]);
			Output = Convert.ToString(reader["Output"]);
			CreatedAt = Convert.ToDateTime(reader["CreatedAt"]);
			Timestamp = Convert.ToDateTime(reader["Timestamp"]);
			Count = Convert.ToInt32(reader["Count"]);
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