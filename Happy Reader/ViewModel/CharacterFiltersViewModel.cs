using System.Windows.Controls;

namespace Happy_Reader.ViewModel
{
	public class CharacterFiltersViewModel : FiltersViewModelBase
	{
		public override string PermanentFilterJsonFile => StaticMethods.PermanentCharacterFilterJson;
		public override string CustomFiltersJsonFile => StaticMethods.CustomCharacterFiltersJson;
		public override IFilter NewFilter { get; } = new CharacterFilter();
		public override ComboBoxItem[] FilterTypes { get; } = StaticMethods.GetEnumValues(typeof(CharacterFilterType));
		public override CustomFilterBase GetNewFilter() => new CustomCharacterFilter();
	}
}
