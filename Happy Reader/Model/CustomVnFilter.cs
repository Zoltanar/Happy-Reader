using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

namespace Happy_Reader
{
	public class CustomVnFilter : INotifyPropertyChanged
	{
		/// <summary>
		/// Name of custom filter
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// List of filters which must all be true
		/// </summary>
		public ObservableCollection<VnFilter> AndFilters { get; set; } = new ObservableCollection<VnFilter>();
		/// <summary>
		/// List of filters in which at least one must be true
		/// </summary>
		public ObservableCollection<VnFilter> OrFilters { get; set; } = new ObservableCollection<VnFilter>();

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <summary>
		/// Create a custom filter with copies of filters from an existing filter.
		/// </summary>
		/// <param name="existingVnFilter"></param>
		public CustomVnFilter(CustomVnFilter existingVnFilter)
		{
			Name = existingVnFilter.Name;
			AndFilters = new ObservableCollection<VnFilter>();
			foreach (var filter in existingVnFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters = new ObservableCollection<VnFilter>();
			foreach (var filter in existingVnFilter.OrFilters) OrFilters.Add(filter.GetCopy());
			OnPropertyChanged();
		}

		/// <summary>
		/// The filter is overwritten by the passed filter.
		/// </summary>
		/// <param name="customFilter"></param>
		internal void Overwrite(CustomVnFilter customFilter)
		{
			AndFilters.Clear();
			foreach (var filter in customFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters.Clear();
			foreach (var filter in customFilter.OrFilters) OrFilters.Add(filter.GetCopy());
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
				if (andFunctions.Length == 0) return orExpression;
			}
			// ReSharper disable AssignNullToNotNullAttribute
			Expression result = Expression.AndAlso(andExpression, orExpression);
			// ReSharper restore AssignNullToNotNullAttribute
			return result;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
