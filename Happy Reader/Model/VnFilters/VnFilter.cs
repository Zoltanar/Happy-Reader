using System;
using System.Globalization;
using System.Linq;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class VnFilter : IFilter
	{
		private VnFilterType _type;
		private string _typeName;
		private string _stringValue = "";
		[CanBeNull] private Func<double, bool> _intFunc;
		[CanBeNull] private Func<DateTime, bool> _dateTimeFunc;

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
					case OwnedStatus enumValue:
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
					return GetTagsFunction();
				case VnFilterType.Traits:
					var writtenTrait = DumpFiles.GetTrait(IntValue);
					return vn => writtenTrait.InCollection(StaticHelpers.LocalDatabase.GetCharactersTraitsForVn(vn.VNID, true).Select(t => t.TraitId)) != Exclude;
				case VnFilterType.HasFullDate:
					return vn => vn.HasFullDate != Exclude;
				case VnFilterType.GameOwned:
					return vn => vn.IsOwned == (OwnedStatus)IntValue != Exclude;
				case VnFilterType.UserVN:
					return vn => vn.UserVN != null != Exclude;
				case VnFilterType.ReleaseDate:
					return vn => DateFunctionFromString(vn.ReleaseDate) != Exclude;
				case VnFilterType.HasAnime:
					return vn => vn.HasAnime != Exclude;
				case VnFilterType.SuggestionScore:
					return vn => DoubleFunctionFromString(vn.Suggestion.Score) != Exclude;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		

		Func<IDataItem<int>, bool> IFilter.GetFunction()
		{
			//we determine what the filter function is only once, rather than per-item
			var func = GetFunction();
			//can't seem to catch exception thrown here when casting.
			return i => i is ListedVN vn && func(vn);
		}

		private bool DateFunctionFromString(DateTime dtValue)
		{
			if (_dateTimeFunc != null) return _dateTimeFunc(dtValue);
			var firstChar = StringValue[0];
			if (char.IsDigit(firstChar))
			{
				var date = DateTime.ParseExact(StringValue, "yyyyMMdd", CultureInfo.CurrentCulture);
				_dateTimeFunc = i => i == date;
			}
			else
			{
				var date = DateTime.ParseExact(StringValue.Substring(1), "yyyyMMdd", CultureInfo.CurrentCulture);
				_dateTimeFunc = firstChar switch
				{
					'>' => i => i > date,
					'<' => i => i < date,
					'=' => i => i == date,
					_ => throw new InvalidOperationException($"Invalid character for function filter: '{firstChar}'")
				};
			}
			return _dateTimeFunc(dtValue);
		}

		private bool DoubleFunctionFromString(double iValue)
		{
			if (_intFunc != null) return _intFunc(iValue);
			var firstChar = StringValue[0];
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (char.IsDigit(firstChar)) _intFunc = i => i == IntValue;
			else
			{
				var functionIntValue = int.Parse(StringValue.Substring(1));
				_intFunc = firstChar switch
				{
					'>' => i => i > functionIntValue,
					'<' => i => i < functionIntValue,
					'=' => i => i == functionIntValue,
					_ => throw new InvalidOperationException($"Invalid character for function filter: {firstChar}")
				};
			}
			return _intFunc(iValue);
		}

		private Func<ListedVN, bool> GetTagsFunction()
		{
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
			return vn => writtenTag.InCollection(vn.Tags.Select(t => t.TagId)) != Exclude;
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
				case VnFilterType.UserVN:
				case VnFilterType.HasAnime:
					return result;
				case VnFilterType.SuggestionScore:
				case VnFilterType.ReleaseDate:
					return $"{result} {StringValue}";
				case VnFilterType.Length:
					return $"{result} - {(StringValue == null ? "None" : ((LengthFilterEnum)IntValue).GetDescription())}";
				case VnFilterType.ReleaseStatus:
					return $"{result} - {(StringValue == null ? "None" : ((ReleaseStatusEnum)IntValue).GetDescription())}";
				case VnFilterType.Label:
					return $"{result} - {(StringValue == null ? "None" : ((UserVN.LabelKind)IntValue).GetDescription())}";
				case VnFilterType.GameOwned:
					return $"{result} - {(StringValue == null ? "None" : ((OwnedStatus)IntValue).GetDescription())}";
				case VnFilterType.Language:
				case VnFilterType.OriginalLanguage:
					return $"{result} - {CultureInfo.GetCultureInfo(StringValue).DisplayName}";
				case VnFilterType.Tags:
					result += $" - {DumpFiles.GetTag(IntValue).Name}";
					if (AdditionalInt != null) result += $" Score >= {AdditionalInt.Value}";
					return result;
				case VnFilterType.Traits:
					return $"{result} - {DumpFiles.GetTrait(IntValue).Name}";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public IFilter GetCopy()
		{
			var filter = new VnFilter(Type, 0, Exclude) { StringValue = StringValue };
			return filter;
		}
	}
}
