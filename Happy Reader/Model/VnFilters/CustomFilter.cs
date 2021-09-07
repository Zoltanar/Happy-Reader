using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class CustomFilter
	{
		/// <summary>
		/// Name of custom filter.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// List of filters which must all be true.
		/// </summary>
		public ObservableCollection<IFilter> AndFilters { get; set; } = new();

		/// <summary>
		/// List of filters in which at least one must be true, must be saved to <see cref="AndFilters"/> with <see cref="SaveOrGroup"/>
		/// </summary>
		[JsonIgnore]
		public ObservableCollection<IFilter> OrFilters { get; set; } = new();

		/// <inheritdoc />
		public override string ToString() => Name;

		[JsonIgnore]
		public CustomFilter OriginalFilter { get; set; }

		/// <summary>
		/// The filter is overwritten by the passed filter.
		/// </summary>
		public void Overwrite(CustomFilter customFilter)
		{
			AndFilters.Clear();
			foreach (var filter in customFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters.Clear();
			foreach (var filter in customFilter.OrFilters) OrFilters.Add(filter.GetCopy());
		}

		public IEnumerable<IDataItem<int>> GetAllResults(
			VisualNovelDatabase database,
			Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> getAllFunc,
			Func<VisualNovelDatabase, int[], IEnumerable<IDataItem<int>>> getAllWithKeyFunc)
		{
			var results = ProcessGlobalFilters(database, getAllFunc);
			var items = getAllWithKeyFunc(database, results);
			var finalResults = items.Where(GetFunction());
			return finalResults;
		}

		private Func<IDataItem<int>, bool> GetFunction()
		{
			if (AndFilters.Count == 0) return _ => true;
			Func<IDataItem<int>, bool>[] andFunctions = AndFilters.Where(f => !f.IsGlobal).Select(filter => filter.GetFunction()).ToArray();
			return item => andFunctions.All(x => x(item));
		}

		private int[] ProcessGlobalFilters(VisualNovelDatabase database, Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> getAllFunc)
		{
			var globalFilters = AndFilters.Where(f => f.IsGlobal).ToList();
			if (globalFilters.Count == 0) return getAllFunc(database).Select(i => i.Key).ToArray();
			var result = getAllFunc(database).Select(i => i.Key);
			foreach (var filter in globalFilters)
			{
				result = result.Intersect(filter.GetGlobalFunction(getAllFunc)(database));
			}
			return result.ToArray();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Create a custom filter with copies of filters from an existing filter.
		/// </summary>
		public CustomFilter(CustomFilter existingFilter)
		{
			OriginalFilter = existingFilter;
			Name = existingFilter.Name;
			AndFilters = new ObservableCollection<IFilter>();
			foreach (var filter in existingFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters = new ObservableCollection<IFilter>();
			foreach (var filter in existingFilter.OrFilters) OrFilters.Add(filter.GetCopy());
			OnPropertyChanged();
		}

		/// <summary>
		/// Constructor for an empty custom filter
		/// </summary>
		public CustomFilter()
		{
			Name = "Custom Filter";
		}
		
		public CustomFilter(string name)
		{
			Name = name;
		}

		public void SaveOrGroup()
		{
			if (!OrFilters.Any()) return;
			var orFilter = OrFilters.Count == 1 ? OrFilters.First() : new GeneralMultiFilter(true, OrFilters);
			AndFilters.Add(orFilter);
			OrFilters.Clear();
		}

		public CustomFilter GetCopy()
		{
			return new(this);
		}
	}
}