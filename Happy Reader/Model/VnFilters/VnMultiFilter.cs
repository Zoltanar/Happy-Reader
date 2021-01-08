using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class VnMultiFilter : IFilter<ListedVN, VnFilterType>
	{
		/// <summary>
		/// True if it represents an OR group, false if it represents an AND group
		/// </summary>
		public bool IsOrGroup { get; set; }

		public List<IFilter<ListedVN, VnFilterType>> Filters { get; set; }

		[JsonIgnore] public VnFilterType Type { get => VnFilterType.Multi; set => throw new NotSupportedException(); }

		[JsonIgnore] public bool Exclude { get => false; set => throw new NotSupportedException(); }

		[JsonIgnore] public object Value { set => throw new NotSupportedException(); }

		/// <summary>
		/// Create custom filter
		/// </summary>
		public VnMultiFilter(bool isOrGroup, ICollection<IFilter<ListedVN, VnFilterType>> filters)
		{
			IsOrGroup = isOrGroup;
			Filters = filters.ToList();
		}
		
		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		/// <returns></returns>
		public Func<ListedVN, bool> GetFunction()
		{
			if (IsOrGroup) return vn => Filters.Any(f => f.GetFunction()(vn));
			return vn => Filters.All(f => f.GetFunction()(vn));
		}

		public override string ToString()
		{
			string result = IsOrGroup ? "OR Group: " : "AND Group: ";
			return result + string.Join("; ", Filters);
		}

		public IFilter<ListedVN, VnFilterType> GetCopy()
		{
			var filter = new VnMultiFilter(IsOrGroup, Filters);
			return filter;
		}
	}
}