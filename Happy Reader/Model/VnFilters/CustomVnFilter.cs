using System.Collections.ObjectModel;
using System.Linq;

namespace Happy_Reader
{
	public class CustomVnFilter : CustomFilterBase
	{
		/// <summary>
		/// Create a custom filter with copies of filters from an existing filter.
		/// </summary>
		public CustomVnFilter(CustomVnFilter existingVnFilter)
		{
			OriginalFilter = existingVnFilter;
			Name = existingVnFilter.Name;
			AndFilters = new ObservableCollection<IFilter>();
			foreach (var filter in existingVnFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters = new ObservableCollection<IFilter>();
			foreach (var filter in existingVnFilter.OrFilters) OrFilters.Add(filter.GetCopy());
			OnPropertyChanged();
		}
		
		/// <summary>
		/// Constructor for an empty custom filter
		/// </summary>
		public CustomVnFilter()
		{
			Name = "Custom Filter";
		}

		public override void SaveOrGroup()
		{
			if (!OrFilters.Any()) return;
			var orFilter = new VnMultiFilter(true, OrFilters);
			AndFilters.Add(orFilter);
			OrFilters.Clear();
		}

		public override CustomFilterBase GetCopy()
		{
			return new CustomVnFilter(this);
		}
	}

}
