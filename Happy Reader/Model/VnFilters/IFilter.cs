using System;

namespace Happy_Reader
{
	public interface IFilter<in T, TType> where TType : Enum
	{

		public TType Type { get; set; }

		public bool Exclude { get; set; }

		public object Value { set; }

		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		Func<T, bool> GetFunction();

		string ToString();

		IFilter<T, TType> GetCopy();

	}
}