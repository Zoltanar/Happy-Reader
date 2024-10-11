using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	/// <summary>
	/// Key is (TagId, ListedVN_VNID)
	/// </summary>
	public sealed class DbTag : IDataGroupItem<int>
    {
		public int TagId { get; set; }

		public double Score { get; set; }

		public int Spoiler { get; set; }

		public TagCategory? Category { get; private set; }

		public int VNID { get; set; }
		
		public void SetCategory()
		{
			string cat = DumpFiles.GetTag(TagId)?.Cat;
			switch (cat)
			{
				case DumpFiles.ContentTag:
					Category = TagCategory.Content;
					return;
				case DumpFiles.SexualTag:
					Category = TagCategory.Sexual;
					return;
				case DumpFiles.TechnicalTag:
					Category = TagCategory.Technical;
					return;
				default:
					return;
			}
		}

		/// <summary>
		/// Return string with Tag name and score, if tag isn't found in list, "Not Approved" is returned.
		/// </summary>
		/// <returns>String with tag name and score</returns>
		public string Print()
		{
			var name = DumpFiles.GetTag(TagId)?.Name;
			return name != null ? $"{name} ({Score:0.00})" : "Not Approved";
		}

		#region IDataItem Implementation
		public int GroupKey => VNID;

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $@"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO DbTags (TagId,Spoiler,ListedVN_VNID,Score,Category) VALUES (@TagId,@Spoiler,@ListedVN_VNID,@Score,@Category)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@TagId", TagId);
			command.AddParameter("@Spoiler", Spoiler);
			command.AddParameter("@ListedVN_VNID", VNID);
			command.AddParameter("@Score", Score);
			command.AddParameter("@Category", Category);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			if (reader == null) throw new ArgumentNullException(nameof(reader));
			try
			{
				VNID = Convert.ToInt32(reader["ListedVN_VNID"]);
				TagId = Convert.ToInt32(reader["TagId"]);
				Spoiler = Convert.ToInt32(reader["Spoiler"]);
				Score = Convert.ToDouble(reader["Score"]);
				Category = (TagCategory?)StaticHelpers.GetNullableInt(reader["Category"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}
		#endregion

		public override string ToString() => $"[{TagId}] Score: {Score:N2}, Spoiler: {Spoiler}";

		/// <summary>
		/// Categories of VN Tags
		/// </summary>
		public enum TagCategory
		{
			Null,
			Content,
			Sexual,
			Technical
		}
	}
}