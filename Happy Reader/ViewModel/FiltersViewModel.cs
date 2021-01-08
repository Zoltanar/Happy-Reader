using Happy_Apps_Core.Database;

namespace Happy_Reader.ViewModel
{
	public class FiltersViewModel : FiltersViewModelBase<CustomVnFilter,ListedVN, VnFilterType>
	{
		public override string PermanentFilterJsonFile => StaticMethods.PermanentFilterJson;
		public override string CustomFiltersJsonFile => StaticMethods.CustomFiltersJson;
		public override IFilter<ListedVN, VnFilterType> NewFilter { get; } = new VnFilter();
	}
}
