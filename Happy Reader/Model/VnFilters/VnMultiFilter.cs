using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

namespace Happy_Reader
{
	public class VnMultiFilter : IFilter<ListedVN>
	{
		/// <summary>
		/// True if it represents an OR group, false if it represents an AND group
		/// </summary>
		public bool IsOrGroup { get; set; }

		public List<IFilter<ListedVN>> Filters { get; set; }

		/// <summary>
		/// Create custom filter
		/// </summary>
		public VnMultiFilter(bool isOrGroup, ICollection<IFilter<ListedVN>> filters)
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

		public IFilter<ListedVN> GetCopy()
		{
			var filter = new VnMultiFilter(IsOrGroup, Filters);
			return filter;
		}
	}
}