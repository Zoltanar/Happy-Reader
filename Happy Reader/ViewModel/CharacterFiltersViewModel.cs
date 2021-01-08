using System.Windows.Controls;
using Happy_Apps_Core;

namespace Happy_Reader.ViewModel
{
	public class CharacterFiltersViewModel : FiltersViewModelBase<CustomCharacterFilter, CharacterItem, CharacterFilterType>
	{
		public override string PermanentFilterJsonFile => StaticMethods.PermanentCharacterFilterJson;
		public override string CustomFiltersJsonFile => StaticMethods.CustomCharacterFiltersJson;
		public override IFilter<CharacterItem, CharacterFilterType> NewFilter { get; } = new CharacterFilter();
	}
}
