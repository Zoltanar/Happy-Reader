using System;
using System.Linq;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class CharacterFilter : IFilter
	{
		private CharacterFilterType _type;
		private string _typeName;
		private string _stringValue = "";
#pragma warning disable 1591

		[JsonIgnore]
		public CharacterFilterType Type
		{
			get => _type;
			set
			{
				_type = value;
				_typeName = value.ToString();
				Value = "";
			}
		}

		[UsedImplicitly]
		public string TypeName
		{
			get => _typeName;
			set
			{
				_typeName = value;
				_type = (CharacterFilterType)Enum.Parse(typeof(CharacterFilterType), value);
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

		[JsonIgnore] private int IntValue { get; set; }

		[JsonIgnore] public object Value
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
		public CharacterFilter(CharacterFilterType type, object value, bool exclude = false)
		{
			Type = type;
			Value = value;
			Exclude = exclude;
		}

		public CharacterFilter()
		{
			Type = 0;
			Value = 0;
			Exclude = false;
		}

		private Func<CharacterItem, bool> GetFunction()
		{
			switch (Type)
			{
				case CharacterFilterType.Traits:
					var writtenTrait = DumpFiles.GetTrait(IntValue);
					return ch => writtenTrait.InCollection(ch.DbTraits.Select(t => t.TraitId)) != Exclude;
				case CharacterFilterType.TraitScore:
					return ch => DoubleFunctionFromString(ch.TraitScore.GetValueOrDefault()) != Exclude;
				case CharacterFilterType.Multi:
					throw new InvalidOperationException();
				case CharacterFilterType.Gender:
					return ch => ch.Gender == StringValue != Exclude;
				case CharacterFilterType.HasImage:
					return ch => ch.ImageId != null != Exclude;
				case CharacterFilterType.HasFullDate:
					return ch => (ch.VisualNovel?.HasFullDate ?? false) != Exclude;
				case CharacterFilterType.UserVN:
					return ch => ch.VisualNovel?.UserVN != null != Exclude;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		Func<IDataItem<int>, bool> IFilter.GetFunction()
		{
			//we determine what the filter function is only once, rather than per-item
			var func = GetFunction();
			//can't seem to catch exception thrown here when casting.
			return i => i is CharacterItem ch && func(ch);
		}

		[CanBeNull] private Func<double, bool> _intFunc;

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
					// ReSharper disable once CompareOfFloatsByEqualityOperator
					'=' => i => i == functionIntValue,
					_ => throw new InvalidOperationException($"Invalid character for function filter: {firstChar}")
				};
			}
			return _intFunc(iValue);
		}

		public override string ToString()
		{
			var typeDesc = Type.GetDescription();
			var result = $"{(Exclude ? "Exclude" : "Include")}: {typeDesc}";
			switch (Type)
			{
				case CharacterFilterType.Gender:
				case CharacterFilterType.TraitScore:
					return $"{result} {StringValue}";
				case CharacterFilterType.Traits:
					return $"{result} - {DumpFiles.GetTrait(IntValue).Name}";
				case CharacterFilterType.Multi:
					throw new InvalidOperationException();
				case CharacterFilterType.UserVN:
				case CharacterFilterType.HasImage:
				case CharacterFilterType.HasFullDate:
					return result;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public IFilter GetCopy()
		{
			var filter = new CharacterFilter(Type, 0, Exclude) { StringValue = StringValue };
			return filter;
		}
	}
}