using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader
{
	public class VnFilter
	{
#pragma warning disable 1591
		public FilterType Type { get; set; }
		public object Value { get; set; }
		public bool Exclude { get; set; }
#pragma warning restore 1591

		/// <summary>
		/// Create custom filter
		/// </summary>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <param name="exclude"></param>
		public VnFilter(FilterType type, object value, bool exclude)
		{
			Type = type;
			Value = value;
			if (type == FilterType.Voted || type == FilterType.Blacklisted || type == FilterType.ByFavoriteProducer)
			{
				Exclude = !(bool)value;
			}
			else Exclude = exclude;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			int intValue = Convert.ToInt32(Value);
			switch (Type)
			{
				case FilterType.Length:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()} - {((LengthFilter)intValue).GetDescription()}";
				case FilterType.ReleaseStatus:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()} - {(UnreleasedFilter)intValue}";
				case FilterType.WishlistStatus:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()} - {(WishlistStatus)intValue}";
				case FilterType.UserlistStatus:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()} - {(UserlistStatus)intValue}";
				case FilterType.Voted:
				case FilterType.Blacklisted:
				case FilterType.ByFavoriteProducer:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()}";
				case FilterType.Language:
				case FilterType.OriginalLanguage:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()} - { CultureInfo.GetCultureInfo((string)Value).DisplayName}";
				case FilterType.Tags:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()} - {DumpFiles.PlainTags.Find(x => x.ID == intValue).Name}";
				case FilterType.Traits:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()} - {DumpFiles.PlainTraits.Find(x => x.ID == intValue).Name}";
				default:
					return $"{(Exclude ? "Exclude" : "Include")}: {Type.GetDescription()} - {Value}";
			}
		}



		/// <summary>
		/// Describes type of filter
		/// </summary>
		public enum FilterType
		{
#pragma warning disable 1591
			Length = 0,
			//ReleasedBetween = 1,
			[Description("Release Status")]
			ReleaseStatus = 2,
			Blacklisted = 3,
			Voted = 4,
			[Description("By Favorite Producer")]
			ByFavoriteProducer = 5,
			[Description("Wishlist Status")]
			WishlistStatus = 6,
			[Description("Userlist Status")]
			UserlistStatus = 7,
			Language = 8,
			[Description("Original Language")]
			OriginalLanguage = 9,
			Tags = 10,
			Traits = 11
#pragma warning restore 1591
		}

		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		/// <returns></returns>
		public Func<ListedVN, bool> GetFunction()
		{
			switch (Type)
			{
				case FilterType.Length:
					var lfValue = GetNullableEnum<LengthFilter?>(Value);
					return vn => vn.LengthTime == lfValue != Exclude;
				case FilterType.ReleaseStatus:
					var ufValue = GetNullableEnum<UnreleasedFilter?>(Value);
					return vn => vn.Unreleased == ufValue != Exclude;
				case FilterType.Voted:
					return vn => vn.Voted != Exclude;
				case FilterType.Blacklisted:
					return vn => vn.Blacklisted != Exclude;
				case FilterType.ByFavoriteProducer:
					return vn => StaticHelpers.VNIsByFavoriteProducer(vn) != Exclude;
				case FilterType.WishlistStatus:
					var wlsValue = GetNullableEnum<WishlistStatus?>(Value);
					return vn => vn.UserVN?.WLStatus == wlsValue != Exclude;
				case FilterType.UserlistStatus:
					var ulsValue = GetNullableEnum<UserlistStatus?>(Value);
					return vn => vn.UserVN?.ULStatus == ulsValue != Exclude;
				case FilterType.Language:
					return vn => vn.HasLanguage((string)Value) != Exclude;
				case FilterType.OriginalLanguage:
					return vn => vn.HasOriginalLanguage((string)Value) != Exclude;
				case FilterType.Tags:
					return vn => vn.DbTags.Any(t=>t.TagId == Convert.ToInt32(Value)) != Exclude;
				case FilterType.Traits: //todo vn.Characters.Traits.Any
					return vn=> true;
					//return vn => vn.MatchesSingleTrait(Convert.ToInt32(Value)) != Exclude;
			}
			return vn => true;
		}

		private static T GetNullableEnum<T>(object oValue)
		{
			if (oValue is T variable) return variable;
			long value = (long) oValue;
			if (value == -1) return default;
			// ReSharper disable once PossibleInvalidCastException
			return (T) oValue;
		}

		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		/// <returns></returns>
		public Expression<Func<ListedVN, bool>> GetExpression()
		{
			switch (Type)
			{
				case FilterType.Length:
					return vn => vn.LengthTime == (LengthFilter)Value != Exclude;
				case FilterType.ReleaseStatus:
					return vn => vn.Unreleased == (UnreleasedFilter)Value != Exclude;
				case FilterType.Voted:
					return vn => vn.Voted != Exclude;
				case FilterType.Blacklisted:
					return vn => vn.Blacklisted != Exclude;
				case FilterType.ByFavoriteProducer:
					return vn => StaticHelpers.VNIsByFavoriteProducer(vn) != Exclude;
				case FilterType.WishlistStatus:
					return vn => (vn.UserVN != null && vn.UserVN.WLStatus == (WishlistStatus)Value) != Exclude;
				case FilterType.UserlistStatus:
					return vn => (vn.UserVN != null && vn.UserVN.ULStatus == (UserlistStatus)Value) != Exclude;
				case FilterType.Language:
					return vn => vn.HasLanguage((string)Value) != Exclude;
				case FilterType.OriginalLanguage:
					return vn => vn.HasOriginalLanguage((string)Value) != Exclude;
				case FilterType.Tags:
					return vn => vn.DbTags.Any(t => t.TagId == Convert.ToInt32(Value)) != Exclude;
				case FilterType.Traits: //todo vn.Characters.Traits.Any
					return vn => true;
				//return vn => vn.MatchesSingleTrait(Convert.ToInt32(Value)) != Exclude;
			}
			return vn => true;
		}
	}
}
