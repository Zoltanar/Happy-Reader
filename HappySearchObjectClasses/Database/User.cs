using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	public sealed class User : IDataItem<int>
	{
		public int Id { get; set; }

		public string Username { get; set; }

		public IEnumerable<ListedProducer> FavoriteProducers => StaticHelpers.LocalDatabase.Producers.WithKeyIn(
			StaticHelpers.LocalDatabase.UserProducers.Where(i => i.User_Id == Id).Select(i => i.ListedProducer_Id).ToArray());

		public override string ToString() => $"[{Id}] {Username}";
		public string KeyField => "Id";
		public int Key => Id;
		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO Users" +
									 "(Id,Username) VALUES " +
									 "(@Id,@Username)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@Id", Id);
			command.AddParameter("@Username", Username);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			if (reader == null) throw new ArgumentNullException(nameof(reader));
			try
			{
				Id = Convert.ToInt32(reader["Id"]);
				Username = Convert.ToString(reader["Username"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}
	}
}