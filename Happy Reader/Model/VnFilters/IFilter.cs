using System;
using System.Collections.Generic;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;

namespace Happy_Reader
{
	public interface IFilter
	{
		public string StringValue { get; set; }

		public bool Exclude { get; set; }

		/// <summary>
		/// Whether the filter should use the global function over the entire database, else, normal function should be used in per-item basis
		/// </summary>
		public bool IsGlobal { get; }

		public object Value { set; }

		/// <summary>
		/// Gets function that determines if item matches filter.
		/// </summary>
		Func<IDataItem<int>, bool> GetFunction();

		/// <summary>
		/// Gets function that returns results from entire database in a single call.
		/// This should return a positive list and ignore <see cref="Exclude"/>, as it is handled by caller.
		/// </summary>
		Func<VisualNovelDatabase, HashSet<int>> GetGlobalFunction(Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> getAllFunc);
		

		IFilter GetCopy();
	}
}