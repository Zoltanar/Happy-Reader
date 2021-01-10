using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public abstract class CustomFilterBase : INotifyPropertyChanged
	{
		/// <summary>
		/// Name of custom filter.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// List of filters which must all be true.
		/// </summary>
		public ObservableCollection<IFilter> AndFilters { get; set; } = new ObservableCollection<IFilter>();

		/// <summary>
		/// List of filters in which at least one must be true, must be saved to <see cref="AndFilters"/> with <see cref="SaveOrGroup"/>
		/// </summary>
		[JsonIgnore]
		public ObservableCollection<IFilter> OrFilters { get; set; } = new ObservableCollection<IFilter>();

		/// <inheritdoc />
		public override string ToString() => Name;

		[JsonIgnore]
		public CustomFilterBase OriginalFilter { get; set; }
		
		/// <summary>
		/// The filter is overwritten by the passed filter.
		/// </summary>
		/// <param name="customFilter"></param>
		public void Overwrite(CustomFilterBase customFilter)
		{
			AndFilters.Clear();
			foreach (var filter in customFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters.Clear();
			foreach (var filter in customFilter.OrFilters) OrFilters.Add(filter.GetCopy());
		}
		public Func<object, bool> GetFunction()
		{
			if (AndFilters.Count == 0) return _ => true;
			Func<object, bool>[] andFunctions = AndFilters.Select(filter => filter.GetFunction()).ToArray();
			return item => andFunctions.All(x => x(item));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public abstract void SaveOrGroup();

		public abstract CustomFilterBase GetCopy();
	}
}