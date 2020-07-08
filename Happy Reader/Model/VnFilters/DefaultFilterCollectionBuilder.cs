using System;
using System.Collections.ObjectModel;
using Happy_Apps_Core.Database;

namespace Happy_Reader.Model.VnFilters
{
	public static class DefaultFilterCollectionBuilder
	{
		public static ObservableCollection<CustomVnFilter> BuildDefaultFilters()
		{
			var filters = new ObservableCollection<CustomVnFilter>();
			BuildLabelFilters(filters);

			foreach (LengthFilterEnum field in Enum.GetValues(typeof(LengthFilterEnum)))
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.Length, field));
				cf.Name = $"Length: {field}";
				filters.Add(cf);
			}

			foreach (ReleaseStatusEnum field in Enum.GetValues(typeof(ReleaseStatusEnum)))
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.ReleaseStatus, field));
				cf.Name = $"Release Status: {field}";
				filters.Add(cf);
			}

			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.ByFavoriteProducer, true));
				cf.Name = "By Favorite Producer";
				filters.Add(cf);
			}
			{
				var cf = new CustomVnFilter();
				cf.OrFilters.Add(new VnFilter(VnFilterType.ReleaseStatus, ReleaseStatusEnum.WithReleaseDate));
				cf.OrFilters.Add(new VnFilter(VnFilterType.ReleaseStatus, ReleaseStatusEnum.Released));
				cf.Name = "With Release Date (All)";
				filters.Add(cf);
			}
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.HasFullDate, true));
				cf.Name = "Has Complete Release Date";
				filters.Add(cf);
			}
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.GameOwned, true));
				cf.Name = "Game is Owned";
				filters.Add(cf);
			}
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.UserVN, true));
				cf.Name = "User-related Title";
				filters.Add(cf);
			}
			return filters;
		}
		
		private static void BuildLabelFilters(ObservableCollection<CustomVnFilter> filters)
		{
			var anyCf = new CustomVnFilter();
			anyCf.AndFilters.Add(new VnFilter(VnFilterType.Label, null, true));
			anyCf.Name = "Label: Any";
			filters.Add(anyCf);
			foreach (UserVN.LabelKind field in Enum.GetValues(typeof(UserVN.LabelKind)))
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.Label, field));
				cf.Name = $"Label: {field}";
				filters.Add(cf);
			}
		}
	}
}
