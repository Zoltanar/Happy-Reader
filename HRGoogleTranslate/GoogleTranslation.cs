using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;

// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace HRGoogleTranslate
{

	public class GoogleTranslation : IDataItem<string>
	{

		public string KeyField { get; } = nameof(Input);
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

		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }
		public string Input { get; set; }
		public string Output { get; set; }
		/// <summary>
		/// Timestamp of creation
		/// </summary>
		public DateTime CreatedAt { get; set; }
		/// <summary>
		/// Timestamp of last used
		/// </summary>
		public DateTime Timestamp { get; set; }
		/// <summary>
		/// Count of times used
		/// </summary>
		public int Count { get; set; }

		public GoogleTranslation(string input, string output)
		{
			Input = input;
			Output = output;
			CreatedAt = DateTime.UtcNow;
			Timestamp = DateTime.UtcNow;
			Count = 1;
		}

		/// <summary>
		/// Increase count by 1 and update timestamp.
		/// </summary>
		public void Update()
		{
			Count++;
			Timestamp = DateTime.UtcNow;
		}

		public GoogleTranslation() { }
	}
}