using System.Collections.ObjectModel;
using System.Linq;
using Happy_Apps_Core;

namespace Happy_Reader
{
	public class CustomCharacterFilter : CustomFilter<CharacterItem, CharacterFilterType>
	{
		/// <summary>
		/// Create a custom filter with copies of filters from an existing filter.
		/// </summary>
		public CustomCharacterFilter(CustomCharacterFilter existingFilter)
		{
			OriginalFilter = existingFilter;
			Name = existingFilter.Name;
			AndFilters = new ObservableCollection<IFilter<CharacterItem, CharacterFilterType>>();
			foreach (var filter in existingFilter.AndFilters) AndFilters.Add(filter.GetCopy());
			OrFilters = new ObservableCollection<IFilter<CharacterItem, CharacterFilterType>>();
			foreach (var filter in existingFilter.OrFilters) OrFilters.Add(filter.GetCopy());
			OnPropertyChanged();
		}

		/// <summary>
		/// Constructor for an empty custom filter
		/// </summary>
		public CustomCharacterFilter()
		{
			Name = "Custom Filter";
		}

		public CustomCharacterFilter(string name, IFilter<CharacterItem, CharacterFilterType> singleFilter)
		{
			Name = name;
			AndFilters = new ObservableCollection<IFilter<CharacterItem, CharacterFilterType>> { singleFilter };
		}

		public override void SaveOrGroup()
		{
			if (!OrFilters.Any()) return;
			var orFilter = new CharacterMultiFilter(true, OrFilters);
			AndFilters.Add(orFilter);
			OrFilters.Clear();
		}

		public override CustomFilter<CharacterItem, CharacterFilterType> GetCopy() => new CustomCharacterFilter(this);
	}
}