using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class VnMultiFilter : IFilter
	{
		/// <summary>
		/// True if it represents an OR group, false if it represents an AND group
		/// </summary>
		public bool IsOrGroup { get; set; }

		public List<IFilter> Filters { get; set; }

		[JsonIgnore] public bool Exclude { get => false; set => throw new NotSupportedException(); }

		[JsonIgnore] public object Value { set => throw new NotSupportedException(); }

		/// <summary>
		/// Create custom filter
		/// </summary>
		public VnMultiFilter(bool isOrGroup, ICollection<IFilter> filters)
		{
			IsOrGroup = isOrGroup;
			Filters = filters.ToList();
		}
		
		public Func<object, bool> GetFunction()
		{
			if (IsOrGroup) return vn => Filters.Any(f => f.GetFunction()(vn));
			return vn => Filters.All(f => f.GetFunction()(vn));
		}

		public override string ToString()
		{
			string result = IsOrGroup ? "OR Group: " : "AND Group: ";
			return result + string.Join("; ", Filters);
		}

		public IFilter GetCopy()
		{
			var filter = new VnMultiFilter(IsOrGroup, Filters);
			return filter;
		}
	}
}