using System.Collections.ObjectModel;
using System.Linq;

namespace Happy_Reader
{
	public class CustomCharacterFilter : CustomFilterBase
	{
		/// <summary>
		/// Create a custom filter with copies of filters from an existing filter.
		/// </summary>
		public CustomCharacterFilter(CustomCharacterFilter existingFilter)
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
		public CustomCharacterFilter()
		{
			Name = "Custom Filter";
		}

		public override void SaveOrGroup()
		{
			if (!OrFilters.Any()) return;
			var orFilter = new CharacterMultiFilter(true, OrFilters);
			AndFilters.Add(orFilter);
			OrFilters.Clear();
		}

		public override CustomFilterBase GetCopy() => new CustomCharacterFilter(this);
	}
}