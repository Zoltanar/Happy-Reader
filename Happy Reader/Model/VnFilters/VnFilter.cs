using System;
using System.Globalization;
using System.Linq;
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
					case UserVN.LabelKind enumValue:
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
					case DateTime dateValue:
						_stringValue = dateValue.ToString("yyyyMMdd");
						IntValue = int.Parse(_stringValue);
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
		public int? AdditionalInt { get; set; }
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
					return vn => (vn.UserVN?.Blacklisted ?? false) != Exclude;
				case VnFilterType.ByFavoriteProducer:
					return vn => StaticHelpers.VNIsByFavoriteProducer(vn) != Exclude;
				case VnFilterType.Label:
					return vn => (vn.UserVN?.Labels.Any(l => l == (UserVN.LabelKind)IntValue) ?? false) != Exclude;
				case VnFilterType.Language:
					return vn => vn.HasLanguage(StringValue) != Exclude;
				case VnFilterType.OriginalLanguage:
					return vn => vn.HasOriginalLanguage(StringValue) != Exclude;
				case VnFilterType.Tags:
					var writtenTag = DumpFiles.GetTag(IntValue);
					//todo make it better, allow handling arrays
					if (AdditionalInt != null)
					{
						return vn =>
						{
							var contains = writtenTag.InCollection(vn.Tags.Select(t => t.TagId), out int match);
							return (!contains || vn.Tags.First(t => t.TagId == match).Score >= AdditionalInt.Value) != Exclude;
						};
					}
					else return vn => writtenTag.InCollection(vn.Tags.Select(t => t.TagId)) != Exclude;
				case VnFilterType.Traits:
					var writtenTrait = DumpFiles.GetTrait(IntValue);
					return vn => writtenTrait.InCollection(StaticHelpers.LocalDatabase.GetCharactersTraitsForVn(vn.VNID,true).Select(t => t.TraitId)) != Exclude;
				case VnFilterType.HasFullDate:
					return vn => vn.HasFullDate != Exclude;
				case VnFilterType.GameOwned:
					return vn => vn.IsOwned == (OwnedStatus)IntValue != Exclude;
				case VnFilterType.UserVN:
					return vn => vn.UserVN != null != Exclude;
				case VnFilterType.ReleasedAfter:
					var afterDate = DateTime.ParseExact(StringValue, "yyyyMMdd", CultureInfo.CurrentCulture);
					return vn => vn.ReleaseDate > afterDate != Exclude;
				case VnFilterType.ReleasedBefore:
					var beforeDate = DateTime.ParseExact(StringValue, "yyyyMMdd", CultureInfo.CurrentCulture);
					return vn => vn.ReleaseDate < beforeDate != Exclude;
				case VnFilterType.HasAnime:
					return vn => vn.HasAnime != Exclude;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public override string ToString()
		{
			var typeDesc = Type.GetDescription();
			var result = $"{(Exclude ? "Exclude" : "Include")}: {typeDesc}";
			switch (Type)
			{
				case VnFilterType.Voted:
				case VnFilterType.Blacklisted:
				case VnFilterType.ByFavoriteProducer:
				case VnFilterType.HasFullDate:
				case VnFilterType.GameOwned:
				case VnFilterType.UserVN:
				case VnFilterType.HasAnime:
					return result;
				case VnFilterType.Length:
					return $"{result} - {(StringValue == null ? "None" : ((LengthFilterEnum)IntValue).GetDescription())}";
				case VnFilterType.ReleaseStatus:
					return $"{result} - {(StringValue == null ? "None" : ((ReleaseStatusEnum)IntValue).GetDescription())}";
				case VnFilterType.Label:
					return $"{result} - {(StringValue == null ? "None" : ((UserVN.LabelKind)IntValue).GetDescription())}";
				case VnFilterType.Language:
				case VnFilterType.OriginalLanguage:
					return $"{result} - {CultureInfo.GetCultureInfo(StringValue).DisplayName}";
				case VnFilterType.Tags:
					result += $" - {DumpFiles.GetTag(IntValue).Name}";
					if (AdditionalInt != null) result += $" Score >= {AdditionalInt.Value}";
					return result;
				case VnFilterType.Traits:
					return $"{result} - {DumpFiles.GetTrait(IntValue).Name}";
				case VnFilterType.ReleasedAfter:
				case VnFilterType.ReleasedBefore:
					return $"{result} - {DateTime.ParseExact(StringValue, "yyyyMMdd", CultureInfo.CurrentCulture)}"; default:
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
