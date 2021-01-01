using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class CustomVnFilter : INotifyPropertyChanged
	{
		/// <summary>
		/// Name of custom filter.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// List of filters which must all be true.
		/// </summary>
		public ObservableCollection<IFilter<ListedVN>> AndFilters { get; set; } = new ObservableCollection<IFilter<ListedVN>>();

		/// <summary>
		/// List of filters in which at least one must be true, must be saved to <see cref="AndFilters"/> with <see cref="SaveOrGroup"/>
		/// </summary>
		[JsonIgnore]
		public ObservableCollection<IFilter<ListedVN>> OrFilters { get; set; } = new ObservableCollection<IFilter<ListedVN>>();

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <summary>
		/// Create a custom filter with copies of filters from an existing filter.
		/// </summary>
		/// <param name="existingVnFilter"></param>
		public CustomVnFilter(CustomVnFilter existingVnFilter)
		{
			OriginalFilter = existingVnFilter;
			Name = existingVnFilter.Name;
			AndFilters = new ObservableCollection<IFilter<ListedVN>>();
			foreach (var filter in existingVnFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters = new ObservableCollection<IFilter<ListedVN>>();
			foreach (var filter in existingVnFilter.OrFilters) OrFilters.Add(filter.GetCopy());
			OnPropertyChanged();
		}

		[JsonIgnore]
		public CustomVnFilter OriginalFilter { get; }

		/// <summary>
		/// The filter is overwritten by the passed filter.
		/// </summary>
		/// <param name="customFilter"></param>
		internal void Overwrite(CustomVnFilter customFilter)
		{
			AndFilters.Clear();
			foreach (var filter in customFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters.Clear();
			foreach (var filter in customFilter.OrFilters) OrFilters.Add((VnFilter)filter.GetCopy());
		}

		/// <summary>
		/// Constructor for an empty custom filter
		/// </summary>
		public CustomVnFilter()
		{
			Name = "Custom Filter";
		}

		public CustomVnFilter(string name, IFilter<ListedVN> singleFilter)
		{
			Name = name;
			AndFilters = new ObservableCollection<IFilter<ListedVN>> { singleFilter };
		}

		public Func<ListedVN, bool> GetFunction()
		{
			if (AndFilters.Count == 0) return vn => true;
			Func<ListedVN, bool>[] andFunctions = AndFilters.Select(filter => filter.GetFunction()).ToArray();
			return vn => andFunctions.All(x => x(vn));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void SaveOrGroup()
		{
			if (!OrFilters.Any()) return;
			var orFilter = new VnMultiFilter(true, OrFilters);
			AndFilters.Add(orFilter);
			OrFilters.Clear();
		}
	}
}
