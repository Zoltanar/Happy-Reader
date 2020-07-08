using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
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
			OriginalFilter = existingVnFilter;
			Name = existingVnFilter.Name;
			AndFilters = new ObservableCollection<VnFilter>();
			foreach (var filter in existingVnFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters = new ObservableCollection<VnFilter>();
			foreach (var filter in existingVnFilter.OrFilters) OrFilters.Add(filter.GetCopy());
			OnPropertyChanged();
		}

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

		private Func<ListedVN, bool> CombineIncludeTagFilters()
		{
			var tags = AndFilters.Where(f => f.Type == VnFilterType.Tags && f.AdditionalInt == null && !f.Exclude)
				.Select(f => (TagId: f.IntValue, Score: f.AdditionalInt, f.Exclude)).ToList();
			var includeTags = tags.Where(t => !t.Exclude).ToList();
			var excludeTags = tags.Where(t => t.Exclude).ToList();
			//factor in exclude

			return vn =>
			{
				var includeTagsCopy = includeTags.ToList();
				foreach (var vnTag in vn.Tags)
				{
					foreach (var t in includeTags)
					{
						//if matched and score is not required or is higher or equal to required, remove from list.
						if (DumpFiles.GetTag(t.TagId).AllIDs.Contains(vnTag.TagId) && t.Score == default || vnTag.Score >= t.Score) includeTagsCopy.Remove(t);
					}
					foreach (var t in excludeTags)
					{
						//if a tag to exclude is found, return false immediately.
						if (DumpFiles.GetTag(t.TagId).AllIDs.Contains(vnTag.TagId)) return false;
					}
				}
				//all matched so all were removed
				return includeTagsCopy.Count == 0;
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
