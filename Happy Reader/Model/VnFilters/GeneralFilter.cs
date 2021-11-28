using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class GeneralFilter : IFilter
	{
		private GeneralFilterType _type;
		private string _typeName;
		private string _stringValue = "";
		[CanBeNull] private Func<double, bool> _doubleFunc;
		[CanBeNull] private Func<DateTime, bool> _dateTimeFunc;

		[JsonIgnore]
		public GeneralFilterType Type
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
				_type = (GeneralFilterType)Enum.Parse(typeof(GeneralFilterType), value);
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
				if (value != null && value.GetType().IsEnum)
				{
					IntValue = (int) value;
					_stringValue = IntValue.ToString();
					return;
				}
				switch (value)
				{
					case IDataItem<int> dataItem:
						IntValue = dataItem.Key;
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
					case DumpFiles.ItemWithParents dumpItemValue:
						IntValue = dumpItemValue.ID;
						_stringValue = IntValue.ToString();
						break;
					case string sValue:
						IntValue = 0;
						_stringValue = sValue;
						break;
					case null:
						IntValue = 0;
						_stringValue = null;
						break;
					default:
						IntValue = 0;
						_stringValue = value.ToString();
						break;
				}
			}
		}

		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool Exclude { get; set; }

		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public int? AdditionalInt { get; set; }
#pragma warning restore 1591
		//when more global filters are added, this should be changed to a switch pattern.
		[JsonIgnore] public bool IsGlobal => Type == GeneralFilterType.Staff || Type == GeneralFilterType.Seiyuu; 

		/// <summary>
		/// Create custom filter
		/// </summary>
		public GeneralFilter(GeneralFilterType type, object value, bool exclude = false)
		{
			Type = type;
			Value = value;
			Exclude = exclude;
		}

		public GeneralFilter()
		{
			Type = 0;
			Value = 0;
			Exclude = false;
		}

		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		private Func<IDataItem<int>, bool> GetFunction()
		{
			switch (Type)
			{
				case GeneralFilterType.Length:
					return i => (GetVisualNovel(i, out var vn) && vn.LengthTime == (LengthFilterEnum)IntValue) != Exclude;
				case GeneralFilterType.ReleaseStatus:
					return i => (GetVisualNovel(i, out var vn) && vn.ReleaseStatus == (ReleaseStatusEnum)IntValue) != Exclude;
				case GeneralFilterType.Voted:
					return i => (GetVisualNovel(i, out var vn) && vn.Voted) != Exclude;
				case GeneralFilterType.ByFavoriteProducer:
					return i => (GetVisualNovel(i, out var vn) && StaticHelpers.VNIsByFavoriteProducer(vn)) != Exclude;
				case GeneralFilterType.Label:
					return i => (GetVisualNovel(i, out var vn) && (vn.UserVN?.Labels.Any(l => l == (UserVN.LabelKind)IntValue) ?? false)) != Exclude;
				case GeneralFilterType.Language:
					return i => (GetVisualNovel(i, out var vn) && vn.HasLanguage(StringValue)) != Exclude;
				case GeneralFilterType.OriginalLanguage:
					return i => (GetVisualNovel(i, out var vn) && vn.HasOriginalLanguage(StringValue)) != Exclude;
				case GeneralFilterType.Tags:
					return TagsFunction;
				case GeneralFilterType.Traits:
					return TraitsFunction;
				case GeneralFilterType.HasFullDate:
					return i => (GetVisualNovel(i, out var vn) && vn.HasFullDate) != Exclude;
				case GeneralFilterType.GameOwned:
					return i => (GetVisualNovel(i, out var vn) && vn.IsOwned == (OwnedStatus)IntValue) != Exclude;
				case GeneralFilterType.UserVN:
					return i => (GetVisualNovel(i, out var vn) && vn.UserVN != null) != Exclude;
				case GeneralFilterType.ReleaseDate:
					return i => (GetVisualNovel(i, out var vn) && DateFunctionFromString(vn.ReleaseDate)) != Exclude;
				case GeneralFilterType.HasAnime:
					return i => (GetVisualNovel(i, out var vn) && vn.HasAnime) != Exclude;
				case GeneralFilterType.SuggestionScore:
					return i => (GetVisualNovel(i, out var vn) && DoubleFunctionFromString(vn.Suggestion.Score)) != Exclude;
				case GeneralFilterType.Seiyuu:
				case GeneralFilterType.Staff:
					throw new InvalidOperationException("This is a global filter.");
				// ReSharper disable once RedundantCaseLabel
				case GeneralFilterType.CharacterTraitScore:
					return GetCharacterTraitScore;
				case GeneralFilterType.CharacterGender:
					return i => ((i is CharacterItem ch && ch.Gender == StringValue) ||
											 (i is ListedVN vn && StaticHelpers.LocalDatabase.GetCharactersForVN(vn.VNID).Any(chara => chara.Gender == StringValue)))
											!= Exclude;
				case GeneralFilterType.CharacterHasImage:
					return i => ((i is CharacterItem ch && ch.ImageId != null) ||
											 (i is ListedVN vn && StaticHelpers.LocalDatabase.GetCharactersForVN(vn.VNID).Any(chara => chara.ImageId != null)))
											!= Exclude;
				case GeneralFilterType.Producer:
					return i => (GetVisualNovel(i, out var vn) && vn.ProducerID == IntValue) != Exclude;
				case GeneralFilterType.Name:
					return SearchByText;
				case GeneralFilterType.VNID:
					return i => (GetVisualNovel(i, out var vn) && vn.VNID == IntValue) != Exclude;
				case GeneralFilterType.NewlyAdded:
					return i => (GetVisualNovel(i, out var vn) && vn.NewSinceUpdate) != Exclude;
				default: throw new ArgumentOutOfRangeException();
			}
		}

		private bool GetCharacterTraitScore(IDataItem<int> item)
		{
			return item switch
			{
				ListedVN vn => DoubleFunctionFromString(vn.Suggestion?.TraitScore ?? 0) != Exclude,
				CharacterItem character => DoubleFunctionFromString(character.TraitScore.GetValueOrDefault()) != Exclude,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private bool SearchByText(IDataItem<int> item)
		{
			return item switch
			{
				ListedVN vn => VisualNovelDatabase.SearchForVN(StringValue)(vn) != Exclude,
				CharacterItem character => VisualNovelDatabase.SearchForCharacter(StringValue)(character) != Exclude,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private bool TraitsFunction(IDataItem<int> item)
		{
			var writtenTrait = DumpFiles.GetTrait(IntValue);
			if (writtenTrait == null) return Exclude;
			IEnumerable<DbTrait> allTraits = item switch
			{
				CharacterItem character => character.DbTraits,
				ListedVN vn => StaticHelpers.LocalDatabase.GetCharactersTraitsForVn(vn.VNID, true),
				_ => throw new ArgumentOutOfRangeException()
			};
			return writtenTrait.InCollection(allTraits.Select(t => t.TraitId)) != Exclude;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool GetVisualNovel(IDataItem<int> item, out ListedVN vn)
		{
			vn = (item as ListedVN ?? (item as CharacterItem)?.VisualNovel);
			return vn != null;
		}

		public Func<VisualNovelDatabase, HashSet<int>> GetGlobalFunction(Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> getAllFunc)
		{
			switch (Type)
			{
				case GeneralFilterType.Staff:
					var staff1 = StaticHelpers.LocalDatabase.StaffAliases[IntValue];
					return db =>
					{
						if (staff1 == null) return new HashSet<int>();
						var first = getAllFunc(db).FirstOrDefault();
						if (first is null) return new HashSet<int>();
						if (first is ListedVN) return db.GetVnsWithStaff(staff1.StaffID);
						if (first is CharacterItem) return db.GetCharactersForVnWithStaff(staff1.StaffID);
						throw new InvalidOperationException($"Unsupported item type for filter: {first.GetType()}");
					};
				case GeneralFilterType.Seiyuu:
					var staff2 = StaticHelpers.LocalDatabase.StaffAliases[IntValue];
					return db =>
					{
						if (staff2 == null) return new HashSet<int>();
						var first = getAllFunc(db).FirstOrDefault();
						if (first is null) return new HashSet<int>();
						if (first is ListedVN) return db.GetVnsWithSeiyuu(staff2.StaffID);
						if (first is CharacterItem) return db.GetCharactersForSeiyuu(staff2.StaffID);
						throw new InvalidOperationException($"Unsupported item type for filter: {first.GetType()}");
					};
				default:
					throw new InvalidOperationException($"Filter type {Type} should use per-item function.");
			}
		}
		
		Func<IDataItem<int>, bool> IFilter.GetFunction()
		{
			//we determine what the filter function is only once, rather than per-item
			var func = GetFunction();
			//can't seem to catch exception thrown here when casting.
			return i => func(i);
		}
		
		private bool DateFunctionFromString(DateTime dtValue)
		{
			if (_dateTimeFunc != null) return _dateTimeFunc(dtValue);
			if (StringValue.StartsWith(">="))
			{
				var compareValue = DateTime.ParseExact(StringValue.Substring(2).Trim(), "yyyyMMdd", CultureInfo.CurrentCulture);
				_dateTimeFunc = i => i >= compareValue;
			}
			else if (StringValue.StartsWith("<="))
			{
				var compareValue = DateTime.ParseExact(StringValue.Substring(2).Trim(), "yyyyMMdd", CultureInfo.CurrentCulture);
				_dateTimeFunc = i => i <= compareValue;
			}
			else if (StringValue.StartsWith(">"))
			{
				var compareValue = DateTime.ParseExact(StringValue.Substring(1).Trim(), "yyyyMMdd", CultureInfo.CurrentCulture);
				_dateTimeFunc = i => i > compareValue;
			}
			else if (StringValue.StartsWith("<"))
			{
				var compareValue = DateTime.ParseExact(StringValue.Substring(1).Trim(), "yyyyMMdd", CultureInfo.CurrentCulture);
				_dateTimeFunc = i => i < compareValue;
			}
			else if (StringValue.StartsWith("="))
			{
				var compareValue = DateTime.ParseExact(StringValue.Substring(1).Trim(), "yyyyMMdd", CultureInfo.CurrentCulture);
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				_dateTimeFunc = i => i == compareValue;
			}
			else if (char.IsDigit(StringValue[0]))
			{
				var compareValue = DateTime.ParseExact(StringValue.Trim(), "yyyyMMdd", CultureInfo.CurrentCulture);
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				_dateTimeFunc = i => i == compareValue;
			}
			else throw new InvalidOperationException($"Invalid double function filter: {StringValue}");
			return _dateTimeFunc(dtValue);
		}

		private bool DoubleFunctionFromString(double iValue)
		{
			if (_doubleFunc != null) return _doubleFunc(iValue);
			if (StringValue.StartsWith(">="))
			{
				var compareValue = double.Parse(StringValue.Substring(2).Trim());
				_doubleFunc = i => i >= compareValue;
			}
			else if (StringValue.StartsWith("<="))
			{
				var compareValue = double.Parse(StringValue.Substring(2).Trim());
				_doubleFunc = i => i <= compareValue;
			}
			else if (StringValue.StartsWith(">"))
			{
				var compareValue = double.Parse(StringValue.Substring(1).Trim());
				_doubleFunc = i => i > compareValue;
			}
			else if (StringValue.StartsWith("<"))
			{
				var compareValue = double.Parse(StringValue.Substring(1).Trim());
				_doubleFunc = i => i < compareValue;
			}
			else if (StringValue.StartsWith("="))
			{
				var compareValue = double.Parse(StringValue.Substring(1).Trim());
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				_doubleFunc = i => i == compareValue;
			}
			else if (char.IsDigit(StringValue[0]))
			{
				var compareValue = double.Parse(StringValue.Trim());
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				_doubleFunc = i => i == compareValue;
			}
			else throw new InvalidOperationException($"Invalid double function filter: {StringValue}");
			return _doubleFunc(iValue);
		}

		private bool TagsFunction(IDataItem<int> item)
		{
			var writtenTag = DumpFiles.GetTag(IntValue);
			if (writtenTag == null || !GetVisualNovel(item, out var vn)) return Exclude;
			//todo make it better, allow handling arrays
			if (AdditionalInt == null) return writtenTag.InCollection(vn.Tags.Select(t => t.TagId)) != Exclude;
			var contains = writtenTag.InCollection(vn.Tags.Select(t => t.TagId), out int match);
			return (!contains || vn.Tags.First(t => t.TagId == match).Score >= AdditionalInt.Value) != Exclude;
		}
		
		public override string ToString()
		{
			var typeDesc = Type.GetDescription();
			var result = $"{(Exclude ? "Exclude" : "Include")}: {typeDesc}";
			switch (Type)
			{
				case GeneralFilterType.Voted:
				case GeneralFilterType.ByFavoriteProducer:
				case GeneralFilterType.HasFullDate:
				case GeneralFilterType.UserVN:
				case GeneralFilterType.HasAnime:
				case GeneralFilterType.CharacterHasImage:
				case GeneralFilterType.NewlyAdded:
					return result;
				case GeneralFilterType.SuggestionScore:
				case GeneralFilterType.ReleaseDate:
				case GeneralFilterType.CharacterTraitScore:
				case GeneralFilterType.CharacterGender:
				case GeneralFilterType.Name:
				case GeneralFilterType.VNID:
					return $"{result} {StringValue}";
				case GeneralFilterType.Length:
					return $"{result} - {(StringValue == null ? "None" : ((LengthFilterEnum)IntValue).GetDescription())}";
				case GeneralFilterType.ReleaseStatus:
					return $"{result} - {(StringValue == null ? "None" : ((ReleaseStatusEnum)IntValue).GetDescription())}";
				case GeneralFilterType.Label:
					return $"{result} - {(StringValue == null ? "None" : ((UserVN.LabelKind)IntValue).GetDescription())}";
				case GeneralFilterType.GameOwned:
					return $"{result} - {(StringValue == null ? "None" : ((OwnedStatus)IntValue).GetDescription())}";
				case GeneralFilterType.Language:
				case GeneralFilterType.OriginalLanguage:
					return $"{result} - {(StringValue == null ? "Empty" : CultureInfo.GetCultureInfo(StringValue).DisplayName)}";
				case GeneralFilterType.Tags:
					result += $" - {DumpFiles.GetTag(IntValue)?.ToString() ?? "Not Found"}";
					if (AdditionalInt != null) result += $" Score >= {AdditionalInt.Value}";
					return result;
				case GeneralFilterType.Traits:
					return $"{result} - {DumpFiles.GetTrait(IntValue)?.ToString() ?? "Not Found"}";
				case GeneralFilterType.Seiyuu:
				case GeneralFilterType.Staff:
					return $"{result} - {StaticHelpers.LocalDatabase.StaffAliases[IntValue]}";
				case GeneralFilterType.Producer:
					return $"{result} - {StaticHelpers.LocalDatabase.Producers[IntValue]}";
				// ReSharper disable once RedundantCaseLabel
				case GeneralFilterType.Multi:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		public IFilter GetCopy()
		{
			var filter = new GeneralFilter(Type, 0, Exclude) { StringValue = StringValue };
			return filter;
		}
	}
	/// <summary>
	/// Describes type of filter
	/// </summary>
	public enum GeneralFilterType
	{
#pragma warning disable 1591
		[TypeConverter(typeof(LengthFilterEnum))]
		Length = 0,
		//ReleasedBetween = 1,
		[Description("Release Status"), TypeConverter(typeof(ReleaseStatusEnum))]
		ReleaseStatus = 2,
		[TypeConverter(typeof(bool))]
		Voted = 4,
		[Description("By Favorite Producer"), TypeConverter(typeof(bool))]
		ByFavoriteProducer = 5,
		[Description("Label"), TypeConverter(typeof(UserVN.LabelKind))]
		Label = 7,
		[TypeConverter(typeof(string))]
		Language = 8,
		[Description("Original Language"), TypeConverter(typeof(string))]
		OriginalLanguage = 9,
		[TypeConverter(typeof(DumpFiles.WrittenTag))]
		Tags = 10,
		[TypeConverter(typeof(DumpFiles.WrittenTrait))]
		Traits = 11,
		[Description("Has Full Date"), TypeConverter(typeof(bool))]
		HasFullDate = 12,
		[Description("Game Owned"), TypeConverter(typeof(OwnedStatus))]
		GameOwned = 13,
		[Description("Is User-related"), TypeConverter(typeof(bool))]
		UserVN = 14,
		[Description("Release Date"), TypeConverter(typeof(string))]
		ReleaseDate = 15,
		[Description("Has Anime"), TypeConverter(typeof(bool))]
		HasAnime = 17,
		[Description("VN Suggestion Score"), TypeConverter(typeof(string))]
		SuggestionScore = 18,
		[NotMapped]
		Multi = 19,
		[TypeConverter(typeof(StaffItem))]
		Staff = 20,
		[Description("Character Trait Score"), TypeConverter(typeof(string))]
		CharacterTraitScore = 21,
		[Description("Character Gender"), TypeConverter(typeof(string))]
		CharacterGender = 22,
		[Description("Character Has Image"), TypeConverter(typeof(bool))]
		CharacterHasImage = 23,
		[Description("Producer"), TypeConverter(typeof(ListedProducer))]
		Producer = 24,
		[TypeConverter(typeof(VnSeiyuu))]
		Seiyuu = 25,
		[TypeConverter(typeof(string))]
		Name = 26,
		[TypeConverter(typeof(int))]
		VNID = 27,
		[Description("Newly Added"), TypeConverter(typeof(bool))]
		NewlyAdded = 28,
#pragma warning restore 1591
	}
}
