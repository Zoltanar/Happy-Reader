using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class CharacterMultiFilter : IFilter<CharacterItem, CharacterFilterType>
	{
		/// <summary>
		/// True if it represents an OR group, false if it represents an AND group
		/// </summary>
		public bool IsOrGroup { get; set; }

		public List<IFilter<CharacterItem, CharacterFilterType>> Filters { get; set; }

		/// <summary>
		/// Create custom filter
		/// </summary>
		public CharacterMultiFilter(bool isOrGroup, ICollection<IFilter<CharacterItem, CharacterFilterType>> filters)
		{
			IsOrGroup = isOrGroup;
			Filters = filters.ToList();
		}

		[JsonIgnore] public CharacterFilterType Type { get => CharacterFilterType.Multi; set => throw new NotSupportedException(); }

		[JsonIgnore] public bool Exclude { get => false; set => throw new NotSupportedException(); }

		[JsonIgnore] public object Value { set => throw new NotSupportedException(); }


		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		/// <returns></returns>
		public Func<CharacterItem, bool> GetFunction()
		{
			if (IsOrGroup) return vn => Filters.Any(f => f.GetFunction()(vn));
			return item => Filters.All(f => f.GetFunction()(item));
		}

		public override string ToString()
		{
			string result = IsOrGroup ? "OR Group: " : "AND Group: ";
			return result + string.Join("; ", Filters);
		}

		public IFilter<CharacterItem, CharacterFilterType> GetCopy()
		{
			var filter = new CharacterMultiFilter(IsOrGroup, Filters);
			return filter;
		}
	}
}