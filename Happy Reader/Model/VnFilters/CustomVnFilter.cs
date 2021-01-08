using System.Collections.ObjectModel;
using System.Linq;
using Happy_Apps_Core.Database;

namespace Happy_Reader
{
	public class CustomVnFilter : CustomFilter<ListedVN, VnFilterType>
	{
		/// <summary>
		/// Create a custom filter with copies of filters from an existing filter.
		/// </summary>
		public CustomVnFilter(CustomVnFilter existingVnFilter)
		{
			OriginalFilter = existingVnFilter;
			Name = existingVnFilter.Name;
			AndFilters = new ObservableCollection<IFilter<ListedVN, VnFilterType>>();
			foreach (var filter in existingVnFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters = new ObservableCollection<IFilter<ListedVN, VnFilterType>>();
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

		public CustomVnFilter(string name, IFilter<ListedVN, VnFilterType> singleFilter)
		{
			Name = name;
			AndFilters = new ObservableCollection<IFilter<ListedVN, VnFilterType>> { singleFilter };
		}

		public override void SaveOrGroup()
		{
			if (!OrFilters.Any()) return;
			var orFilter = new VnMultiFilter(true, OrFilters);
			AndFilters.Add(orFilter);
			OrFilters.Clear();
		}

		public override CustomFilter<ListedVN, VnFilterType> GetCopy()
		{
			return new CustomVnFilter(this);
		}
	}

}
