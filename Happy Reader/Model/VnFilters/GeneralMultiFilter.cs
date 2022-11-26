using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class GeneralMultiFilter : IFilter
	{
		/// <summary>
		/// True if it represents an OR group, false if it represents an AND group
		/// </summary>
		public bool IsOrGroup { get; set; }

		public List<IFilter> Filters { get; set; }

		[JsonIgnore] public string StringValue { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		[JsonIgnore] public bool Exclude { get => false; set => throw new NotSupportedException(); }
		[JsonIgnore] public object Value { set => throw new NotSupportedException(); }
		[JsonIgnore] public LangRelease LangRelease { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            [JsonIgnore] public bool IsGlobal => Filters.Any(f => f.IsGlobal);

		/// <summary>
		/// Create custom filter
		/// </summary>
		public GeneralMultiFilter(bool isOrGroup, IEnumerable<IFilter> filters)
		{
			IsOrGroup = isOrGroup;
			Filters = filters.ToList();
		}

		public Func<IDataItem<int>, bool> GetFunction()
		{
			var functions = Filters.Select(f => f.GetFunction()).ToArray();
			if (IsOrGroup)
			{
				return item => functions.Any(f => f(item));
			}
			return item => functions.All(f => f(item));
		}

		public Func<VisualNovelDatabase, HashSet<int>> GetGlobalFunction(Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> getAllFunc)
		{
			return db =>
			{
				var result = new HashSet<int>(); 
				Action<IEnumerable<int>> unionOrIntersect = IsOrGroup ? result.UnionWith : result.IntersectWith;
				foreach (var filter in Filters.Where(f => f.IsGlobal))
                {
                    var results = filter.GetGlobalFunction(getAllFunc)(db);
                    if (!IsOrGroup)
                    {
						if(filter.Exclude) result.ExceptWith(results);
						else result.IntersectWith(results);
                    }
                    else
                    {
						//if it is OR group, and filter is to exclude, we union with all that do not match filter
                        result.UnionWith(filter.Exclude ? getAllFunc(db).Select(i => i.Key).Except(results) : results);
                    }
				}
				foreach (var filter in Filters.Where(f => !f.IsGlobal))
				{
					unionOrIntersect(getAllFunc(db).Where(filter.GetFunction()).Select(i => i.Key));
				}
				return result;
			};
		}

		public override string ToString()
		{
			string result = IsOrGroup ? "OR Group: " : "AND Group: ";
			return result + string.Join("; ", Filters);
		}

		public IFilter GetCopy()
		{
			var filter = new GeneralMultiFilter(IsOrGroup, Filters);
			return filter;
		}
	}
}