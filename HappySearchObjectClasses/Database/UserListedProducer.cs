using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	/// <summary>
	/// Keys: (ListedProducer_Id, UserId)
	/// </summary>
	public class UserListedProducer : IDataItem<(int,int)>
	{
		// ReSharper disable InconsistentNaming
		public int ListedProducer_Id { get; set; }
		public int User_Id { get; set; }
		// ReSharper restore InconsistentNaming
		public double UserAverageVote { get; set; }
		public int UserDropRate { get; set; }

		#region IDataItem Implementation
		public string KeyField => "(ListedProducer_Id, UserId)";
		public (int, int) Key => (ListedProducer_Id, User_Id);

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO UserListedProducers" +
									 "(ListedProducer_Id,User_Id,UserAverageVote,UserDropRate) VALUES " +
									 "(@ListedProducer_Id,@User_Id,@UserAverageVote,@UserDropRate)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@ListedProducer_Id", ListedProducer_Id);
			command.AddParameter("@User_Id", User_Id);
			command.AddParameter("@UserAverageVote", UserAverageVote);
			command.AddParameter("@UserDropRate", UserDropRate);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			if (reader == null) throw new ArgumentNullException(nameof(reader));
			try
			{
				ListedProducer_Id = Convert.ToInt32(reader["ListedProducer_Id"]);
				User_Id = Convert.ToInt32(reader["User_Id"]);
				UserAverageVote = Convert.ToDouble(reader["UserAverageVote"]);
				UserDropRate = Convert.ToInt32(reader["UserDropRate"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}
		#endregion
	}
}
