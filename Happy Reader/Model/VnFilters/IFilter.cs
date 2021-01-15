using System;
using Happy_Apps_Core.DataAccess;

namespace Happy_Reader
{
	public interface IFilter
	{
		public string StringValue { get; set; }

		public bool Exclude { get; set; }

		public object Value { set; }

		/// <summary>
		/// Gets function that determines if item matches filter.
		/// </summary>
		Func<IDataItem<int>, bool> GetFunction();

		IFilter GetCopy();
	}
}