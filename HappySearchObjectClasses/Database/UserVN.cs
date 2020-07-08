using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	/// <summary>
	/// Key is UserId, VNID
	/// </summary>
	public sealed class UserVN : IDataItem<(int, int)>
	{
		public int VNID { get; set; }

		public int UserId { get; set; }

		public string ULNote { get; set; }

		public DateTime? Added { get; set; }

		public int? Vote { get; set; }

		public DateTime? VoteAdded { get; set; }

		public HashSet<LabelKind> Labels { get; set; }

		[NotMapped]
		public bool Blacklisted => Labels?.Any(b => b == LabelKind.Blacklist) ?? false;

		public enum LabelKind
		{
			Playing = 1,
			Finished = 2,
			Stalled = 3,
			Dropped = 4,
			Wishlist = 5,
			Blacklist = 6,
			Voted = 7,
			[Description("Wishlist - High")]
			WishlistHigh = 10,
			[Description("Wishlist - Medium")]
			WishlistMedium = 11,
			[Description("Wishlist - Low")]
			WishlistLow = 12,
			Owned = 13,
		}

		#region IDataItem implementation

		string IDataItem<(int, int)>.KeyField => "(UserId,VNID)";

		(int, int) IDataItem<(int, int)>.Key => (UserId, VNID);

		DbCommand IDataItem<(int, int)>.UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO UserVNs(VNID,UserId,ULNote,Vote,VoteAdded, Added, Labels) " +
									 "VALUES (@vnid,@userid,@ulnote,@vote,@voteadded, @added, @labels)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@vnid", VNID);
			command.AddParameter("@userid", UserId);
			command.AddParameter("@ulnote", ULNote);
			command.AddParameter("@vote", Vote);
			command.AddParameter("@voteadded", VoteAdded);
			command.AddParameter("@added", Added);
			command.AddParameter("@labels", string.Join(",", Labels.Cast<int>()));
			return command;
		}

		void IDataItem<(int, int)>.LoadFromReader(System.Data.IDataRecord reader)
		{
			if (reader == null) throw new ArgumentNullException(nameof(reader));
			try
			{
				VNID = Convert.ToInt32(reader["VNID"]);
				UserId = Convert.ToInt32(reader["UserId"]);
				ULNote = StaticHelpers.GetNullableString(reader["ULNote"]);
				Vote = StaticHelpers.GetNullableInt(reader["Vote"]);
				VoteAdded = StaticHelpers.GetNullableDate(reader["VoteAdded"]);
				Added = StaticHelpers.GetNullableDate(reader["Added"]);
				Labels = Convert.ToString(reader["Labels"]).Split(',').Select(i => (LabelKind)int.Parse(i)).ToHashSet();
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