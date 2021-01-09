using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Tiles;

namespace Happy_Reader.ViewModel
{
	public class VNTabViewModel : DatabaseViewModelBase<ListedVN>
	{
		protected override Func<VisualNovelDatabase, DACollection<int, ListedVN>> GetAll => db => db.VisualNovels;
		protected override Func<ListedVN, UserControl> GetTile => VNTile.FromListedVN;
		protected override NamedFunction<ListedVN> DbFunction { get; set; } = new NamedFunction<ListedVN>(x => x.VisualNovels, "All", false);
		protected override Func<IEnumerable<ListedVN>, IEnumerable<ListedVN>> Ordering { get; set; } = lvn => lvn.OrderByDescending(x => x.ReleaseDate);
		protected override Func<ListedVN, bool> IsBlacklistedFunction => vn => vn.UserVN?.Blacklisted ?? false;
		protected override Func<ListedVN, ListedProducer> GetProducer => vn => vn.Producer;
		protected override Func<ListedVN, SuggestionScoreObject> GetSuggestion => vn => vn.Suggestion;
		protected override Func<string, Func<ListedVN, bool>> SearchByText => VisualNovelDatabase.SearchForVN;
		public override FiltersViewModelBase FiltersViewModel { get; } = new FiltersViewModel();

		public VNTabViewModel(MainWindowViewModel mainWindowViewModel) : base(mainWindowViewModel) { }

		public override int[] GetRelatedTitles(ListedVN vn)
		{
			return vn.GetAllRelations().Select(v => v.ID).Concat(new[] { vn.VNID }).ToArray();
		}

		public override async Task SortByRecommended()
		{
			Ordering = lvn => lvn.OrderByDescending(vn => vn.Suggestion?.Score ?? 0d)
				.ThenBy(vn => vn.UserVN == null ? 4 :
					vn.UserVN.Labels.Contains(UserVN.LabelKind.WishlistHigh) ? 1 :
					vn.UserVN.Labels.Contains(UserVN.LabelKind.WishlistMedium) ? 2 :
					vn.UserVN.Labels.Contains(UserVN.LabelKind.WishlistLow) ? 3 : 4)
				.ThenByDescending(x => x.ReleaseDate);
			await RefreshTiles();
		}

		public override async Task SortByMyScore()
		{
			Ordering = lvn => lvn.OrderByDescending(vn => vn.UserVN?.Vote).ThenByDescending(vn => vn.ReleaseDate);
			await RefreshTiles();
		}

		public override async Task SortByRating()
		{
			Ordering = lvn => lvn.OrderByDescending(vn => vn.Rating).ThenByDescending(vn => vn.ReleaseDate);
			await RefreshTiles();
		}

		public override async Task SortByReleaseDate()
		{
			Ordering = lvn => lvn.OrderByDescending(vn => vn.ReleaseDate);
			await RefreshTiles();
		}

		public override async Task SortByTitle()
		{
			Ordering = lvn => lvn.OrderBy(vn => vn.Title).ThenByDescending(vn => vn.ReleaseDate);
			await RefreshTiles();
		}
	}
}