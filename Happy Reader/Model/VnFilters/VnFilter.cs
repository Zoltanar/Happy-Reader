using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class VnFilter
	{
		private VnFilterType _type;
		private string _typeName;
		private string _stringValue = "";
#pragma warning disable 1591

		[JsonIgnore]
		public VnFilterType Type
		{
			get => _type;
			set
			{
				_type = value;
				_typeName = value.ToString();
				Value = "";
			}
		}
		public string TypeName
		{
			get => _typeName;
			set
			{
				_typeName = value;
				_type = (VnFilterType)Enum.Parse(typeof(VnFilterType), value);
			}
		}

		public string StringValue
		{
			get => _stringValue;
			set
			{
				_stringValue = value;
				if (int.TryParse(value, out int intValue)) Value = intValue;
				if (bool.TryParse(value, out bool boolValue)) Value = boolValue;
			}
		}

		[JsonIgnore]
		public int IntValue { get; set; }
		[JsonIgnore]
		public object Value
		{
			set
			{
				switch (value)
				{
					case LengthFilterEnum enumValue:
						IntValue = (int)enumValue;
						_stringValue = IntValue.ToString();
						break;
					case ReleaseStatusEnum enumValue:
						IntValue = (int)enumValue;
						_stringValue = IntValue.ToString();
						break;
					case WishlistStatus enumValue:
						IntValue = (int)enumValue;
						_stringValue = IntValue.ToString();
						break;
					case UserlistStatus enumValue:
						IntValue = (int)enumValue;
						_stringValue = IntValue.ToString();
						break;
					case int intValue:
						IntValue = intValue;
						_stringValue = IntValue.ToString();
						break;
					case bool boolValue:
						IntValue = boolValue ? 1 : 0;
						Exclude = !boolValue;
						_stringValue = boolValue.ToString();
						break;
					default:
						IntValue = 0;
						_stringValue = value?.ToString();
						break;
				}
			}
		}

		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool Exclude { get; set; }

		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public int AdditionalInt { get; set; }
#pragma warning restore 1591

		/// <summary>
		/// Create custom filter
		/// </summary>
		public VnFilter(VnFilterType type, object value, bool exclude = false)
		{
			Type = type;
			Value = value;
			Exclude = exclude;
		}

		public VnFilter()
		{
			Type = 0;
			Value = 0;
			Exclude = false;
		}

		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		/// <returns></returns>
		public Func<ListedVN, bool> GetFunction()
		{
			switch (Type)
			{
				case VnFilterType.Length:
					return vn => vn.LengthTime == (LengthFilterEnum)IntValue != Exclude;
				case VnFilterType.ReleaseStatus:
					return vn => vn.ReleaseStatus == (ReleaseStatusEnum)IntValue != Exclude;
				case VnFilterType.Voted:
					return vn => vn.Voted != Exclude;
				case VnFilterType.Blacklisted:
					return vn => vn.Blacklisted != Exclude;
				case VnFilterType.ByFavoriteProducer:
					return vn => StaticHelpers.VNIsByFavoriteProducer(vn) != Exclude;
				case VnFilterType.WishlistStatus:
					return vn => vn.UserVN?.WLStatus == (WishlistStatus)IntValue != Exclude;
				case VnFilterType.UserlistStatus:
					return vn => vn.UserVN?.ULStatus == (UserlistStatus)IntValue != Exclude;
				case VnFilterType.Language:
					return vn => vn.HasLanguage(StringValue) != Exclude;
				case VnFilterType.OriginalLanguage:
					return vn => vn.HasOriginalLanguage(StringValue) != Exclude;
				case VnFilterType.Tags:
					if (AdditionalInt != 0) return vn => vn.DbTags.Any(t => t.TagId == IntValue && t.Score >= AdditionalInt) != Exclude;
					else return vn => vn.DbTags.Any(t => t.TagId == IntValue) != Exclude;
				case VnFilterType.Traits:
					//todo vn => vn.DbTraits.Any(t => t.TraitId == Value) != Exclude; //return vn => vn.MatchesSingleTrait(Convert.ToInt32(Value)) != Exclude;
					throw new NotImplementedException();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		/// <returns></returns>
		public Expression<Func<ListedVN, bool>> GetExpression()
		{
			switch (Type)
			{
				case VnFilterType.Length:
					return vn => vn.LengthTime == (LengthFilterEnum)IntValue != Exclude;
				case VnFilterType.ReleaseStatus:
					return vn => vn.ReleaseStatus == (ReleaseStatusEnum)IntValue != Exclude;
				case VnFilterType.Voted:
					return vn => vn.Voted != Exclude;
				case VnFilterType.Blacklisted:
					return vn => vn.Blacklisted != Exclude;
				case VnFilterType.ByFavoriteProducer:
					return vn => StaticHelpers.VNIsByFavoriteProducer(vn) != Exclude;
				case VnFilterType.WishlistStatus:
					return vn => (vn.UserVN != null && vn.UserVN.WLStatus == (WishlistStatus)IntValue) != Exclude;
				case VnFilterType.UserlistStatus:
					return vn => (vn.UserVN != null && vn.UserVN.ULStatus == (UserlistStatus)IntValue) != Exclude;
				case VnFilterType.Language:
					return vn => vn.HasLanguage(StringValue) != Exclude;
				case VnFilterType.OriginalLanguage:
					return vn => vn.HasOriginalLanguage(StringValue) != Exclude;
				case VnFilterType.Tags:
					if (AdditionalInt != 0) return vn => vn.DbTags.Any(t => t.TagId == IntValue && t.Score >= AdditionalInt) != Exclude;
					else return vn => vn.DbTags.Any(t => t.TagId == IntValue) != Exclude;
				case VnFilterType.Traits:
					// todo vn => vn.DbTraits.Any(t => t.TraitId == IntValue) != Exclude; //return vn => vn.MatchesSingleTrait(Convert.ToInt32(Value)) != Exclude;
					throw new NotImplementedException();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public override string ToString()
		{
			var typeDesc = Type.GetDescription();
			switch (Type)
			{
				case VnFilterType.Length:
					return $"{(Exclude ? "Exclude" : "Include")}: {typeDesc} - {((LengthFilterEnum)IntValue).GetDescription()}";
				case VnFilterType.ReleaseStatus:
					return $"{(Exclude ? "Exclude" : "Include")}: {typeDesc} - {((ReleaseStatusEnum)IntValue).GetDescription()}";
				case VnFilterType.WishlistStatus:
					return $"{(Exclude ? "Exclude" : "Include")}: {typeDesc} - {((WishlistStatus)IntValue).GetDescription()}";
				case VnFilterType.UserlistStatus:
					return $"{(Exclude ? "Exclude" : "Include")}: {typeDesc} - {((UserlistStatus)IntValue).GetDescription()}";
				case VnFilterType.Voted:
				case VnFilterType.Blacklisted:
				case VnFilterType.ByFavoriteProducer:
					return $"{(Exclude ? "Exclude" : "Include")}: {typeDesc}";
				case VnFilterType.Language:
				case VnFilterType.OriginalLanguage:
					return $"{(Exclude ? "Exclude" : "Include")}: {typeDesc} - { CultureInfo.GetCultureInfo(StringValue).DisplayName}";
				case VnFilterType.Tags:
					var tag = DumpFiles.GetTag(IntValue);
					var result = $"{(Exclude ? "Exclude" : "Include")}: {typeDesc} - {tag.Name}";
					if (AdditionalInt != 0) result += $" Score >= {AdditionalInt}";
					return result;
				case VnFilterType.Traits:
					return $"{(Exclude ? "Exclude" : "Include")}: {typeDesc} - {DumpFiles.PlainTraits.Find(x => x.ID == IntValue).Name}";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public VnFilter GetCopy()
		{
			var filter = new VnFilter(Type, 0, Exclude) { StringValue = StringValue };
			return filter;
		}
	}
}
