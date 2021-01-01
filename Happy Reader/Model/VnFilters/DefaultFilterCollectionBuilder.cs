using System;
using System.Collections.ObjectModel;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader
{
	public static class DefaultFilterCollectionBuilder
	{
		private static void AddFiltersForEnum(ObservableCollection<CustomVnFilter> filters, VnFilterType filterType, Type enumType, string label)
		{
			foreach (Enum field in Enum.GetValues(enumType))
			{
				filters.Add(new CustomVnFilter($"{label}: {field.GetDescription()}", new VnFilter(filterType, field)));
			}
		}

		public static ObservableCollection<CustomVnFilter> BuildDefaultFilters()
		{
			var filters = new ObservableCollection<CustomVnFilter>();
			BuildLabelFilters(filters);
			AddFiltersForEnum(filters, VnFilterType.Length, typeof(LengthFilterEnum), "Length");
			AddFiltersForEnum(filters, VnFilterType.ReleaseStatus, typeof(ReleaseStatusEnum), "Release Status");
			filters.Add(new CustomVnFilter("By Favorite Producer", new VnFilter(VnFilterType.ByFavoriteProducer, true)));
			var withReleaseDateAllFilter = new CustomVnFilter();
			withReleaseDateAllFilter.OrFilters.Add(new VnFilter(VnFilterType.ReleaseStatus, ReleaseStatusEnum.WithReleaseDate));
			withReleaseDateAllFilter.OrFilters.Add(new VnFilter(VnFilterType.ReleaseStatus, ReleaseStatusEnum.Released));
			withReleaseDateAllFilter.SaveOrGroup();
			withReleaseDateAllFilter.Name = "With Release Date (All)";
			filters.Add(withReleaseDateAllFilter);
			filters.Add(new CustomVnFilter("Has Complete Release Date", new VnFilter(VnFilterType.HasFullDate, true)));
			filters.Add(new CustomVnFilter("Game is Owned", new VnFilter(VnFilterType.GameOwned, true)));
			filters.Add(new CustomVnFilter("User-related Title", new VnFilter(VnFilterType.UserVN, true)));
			return filters;
		}

		private static void BuildLabelFilters(ObservableCollection<CustomVnFilter> filters)
		{
			var anyCf = new CustomVnFilter();
			anyCf.AndFilters.Add(new VnFilter(VnFilterType.Label, null, true));
			anyCf.Name = "Label: Any";
			filters.Add(anyCf);
			AddFiltersForEnum(filters, VnFilterType.Label, typeof(UserVN.LabelKind), "Label");
		}
	}
}
