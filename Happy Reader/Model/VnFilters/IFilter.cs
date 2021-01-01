using System;

namespace Happy_Reader
{
	public interface IFilter<in T>
	{
		/// <summary>
		/// Gets function that determines if vn matches filter.
		/// </summary>
		Func<T, bool> GetFunction();

		string ToString();
		IFilter<T> GetCopy();
	}
}