using System.Windows.Controls;

namespace Happy_Reader.ViewModel
{
	public class FiltersViewModel : FiltersViewModelBase
	{
		public override string PermanentFilterJsonFile => StaticMethods.PermanentFilterJson;
		public override string CustomFiltersJsonFile => StaticMethods.CustomFiltersJson;
		public override IFilter NewFilter { get; } = new VnFilter();
		public override ComboBoxItem[] FilterTypes {get;} = StaticMethods.GetEnumValues(typeof(VnFilterType));
		public override CustomFilterBase GetNewFilter() => new CustomVnFilter();
	}
}
