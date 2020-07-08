using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database
{
	/// <summary>
	/// Key is (TagId, ListedVN_VNID)
	/// </summary>
	public sealed class DbTag : IDataItem<(int, int)>
	{
		public int TagId { get; set; }

		public double Score { get; set; }

		public int Spoiler { get; set; }

		public StaticHelpers.TagCategory? Category { get; private set; }

		//todo rename
		public int ListedVN_VNID { get; set; }
		
		public void SetCategory()
		{
			string cat = DumpFiles.GetTag(TagId)?.Cat;
			switch (cat)
			{
				case DumpFiles.ContentTag:
					Category = StaticHelpers.TagCategory.Content;
					return;
				case DumpFiles.SexualTag:
					Category = StaticHelpers.TagCategory.Sexual;
					return;
				case DumpFiles.TechnicalTag:
					Category = StaticHelpers.TagCategory.Technical;
					return;
				default:
					return;
			}
		}

		private sealed class EqualityComparer : IEqualityComparer<DbTag>
		{
			public bool Equals(DbTag x, DbTag y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (x is null) return false;
				if (y is null) return false;
				if (x.GetType() != y.GetType()) return false;
				return x.TagId == y.TagId && x.ListedVN_VNID == y.ListedVN_VNID;
			}

			public int GetHashCode(DbTag obj)
			{
				unchecked
				{
					return (obj.TagId * 397) ^ obj.ListedVN_VNID;
				}
			}
		}

		public static IEqualityComparer<DbTag> KeyComparer { get; } = new EqualityComparer();

		/// <summary>
		/// Return string with Tag name and score, if tag isn't found in list, "Not Approved" is returned.
		/// </summary>
		/// <returns>String with tag name and score</returns>
		public string Print()
		{
			var name = DumpFiles.GetTag(TagId)?.Name;
			return name != null ? $"{name} ({Score:0.00})" : "Not Approved";
		}

		public static DbTag From(VNItem.TagItem tag, int vnid)
		{
			var result = new DbTag
			{
				Score = tag.Score,
				Spoiler = tag.Spoiler,
				TagId = tag.ID,
				ListedVN_VNID = vnid
			};
			result.SetCategory();
			return result;
		}

		#region IDataItem Implementation
		public string KeyField { get; } = "(TagId, ListedVN_VNID)";
		public (int, int) Key => (TagId, ListedVN_VNID);

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $@"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO DbTags (TagId,Spoiler,ListedVN_VNID,Score,Category) VALUES (@TagId,@Spoiler,@ListedVN_VNID,@Score,@Category)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@TagId", TagId);
			command.AddParameter("@Spoiler", Spoiler);
			command.AddParameter("@ListedVN_VNID", ListedVN_VNID);
			command.AddParameter("@Score", Score);
			command.AddParameter("@Category", Category);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			if (reader == null) throw new ArgumentNullException(nameof(reader));
			try
			{
				ListedVN_VNID = Convert.ToInt32(reader["ListedVN_VNID"]);
				TagId = Convert.ToInt32(reader["TagId"]);
				Spoiler = Convert.ToInt32(reader["Spoiler"]);
				Score = Convert.ToDouble(reader["Score"]);
				Category = (StaticHelpers.TagCategory?)StaticHelpers.GetNullableInt(reader["Category"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}
		#endregion

		public override string ToString() => $"[{TagId}] Score: {Score:N2}, Spoiler: {Spoiler}";
	}
}