using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader
{
	public class CustomVnFilter
	{
		/// <summary>
		/// Name of custom filter
		/// </summary>
		public string Name;

		/// <summary>
		/// List of filters which must all be true
		/// </summary>
		public readonly BindingList<VnFilter> AndFilters = new BindingList<VnFilter>();
		/// <summary>
		/// List of filters in which at least one must be true
		/// </summary>
		public readonly BindingList<VnFilter> OrFilters = new BindingList<VnFilter>();

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <summary>
		/// Create a custom filter with copies of filters from an existing filter.
		/// </summary>
		/// <param name="existingVnFilter"></param>
		public CustomVnFilter(CustomVnFilter existingVnFilter)
		{
			Name = existingVnFilter.Name;
			AndFilters = new BindingList<VnFilter>();
			AndFilters.AddRange(existingVnFilter.AndFilters.ToArray());
			OrFilters = new BindingList<VnFilter>();
			OrFilters.AddRange(existingVnFilter.OrFilters.ToArray());
		}

		/// <summary>
		/// Constructor for an empty custom filter
		/// </summary>
		public CustomVnFilter()
		{
			Name = "Custom Filter";
		}

		public Func<ListedVN, bool> GetFunction()
		{
			Func<ListedVN, bool>[] andFunctions = AndFilters.Select(filter => filter.GetFunction()).ToArray();
			Func<ListedVN, bool>[] orFunctions = OrFilters.Select(filter => filter.GetFunction()).ToArray();
			//if all and functions are true and 1+ or function is true
			if (andFunctions.Length + orFunctions.Length == 0) return vn => true;
			if (andFunctions.Length > 0 && orFunctions.Length == 0) return vn => andFunctions.All(x => x(vn));
			if (andFunctions.Length == 0 && orFunctions.Length > 0) return vn => orFunctions.Any(x => x(vn));
			return vn => andFunctions.All(x => x(vn)) && orFunctions.Any(x => x(vn));
		}

		public Expression GetExpression()
		{
			//Func<ListedVN, bool>[] andFunctions = AndFilters.Select(filter => filter.GetExpression().Compile()).ToArray();
			//Func<ListedVN, bool>[] orFunctions = OrFilters.Select(filter => filter.GetExpression().Compile()).ToArray();
			Expression<Func<ListedVN, bool>>[] andFunctions = AndFilters.Select(filter => filter.GetExpression()).ToArray();
			Expression<Func<ListedVN, bool>>[] orFunctions = OrFilters.Select(filter => filter.GetExpression()).ToArray();
			if (andFunctions.Length + orFunctions.Length == 0) return Expression.Constant(true);
			Expression andExpression = null;
			Expression orExpression = null;
			if (andFunctions.Length > 0)
			{
				andExpression = andFunctions[0].Body;
				var index = 1;
				while (index < andFunctions.Length)
				{
					andExpression = Expression.AndAlso(andExpression, andFunctions[index].Body);
					index++;
				}
				if (orFunctions.Length == 0) return andExpression;
			}
			//if all and functions are true and 1+ or function is true
			if (orFunctions.Length > 0)
			{
				orExpression = andFunctions[0].Body;
				var index = 1;
				while (index < andFunctions.Length)
				{
					orExpression = Expression.Or(orExpression, andFunctions[index].Body);
					index++;
				}
				if (andFunctions.Length == 0 ) return  orExpression;
			}
			// ReSharper disable AssignNullToNotNullAttribute
			Expression result = Expression.AndAlso(andExpression, orExpression);
			// ReSharper restore AssignNullToNotNullAttribute
			return result;
		}
	}
}
