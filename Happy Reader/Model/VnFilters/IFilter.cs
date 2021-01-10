using System;

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
		Func<object, bool> GetFunction();

		IFilter GetCopy();
	}
}