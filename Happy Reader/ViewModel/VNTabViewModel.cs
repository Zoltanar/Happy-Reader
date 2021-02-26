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
	public class VNTabViewModel : DatabaseViewModelBase
	{
		protected override Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> GetAll => db => db.VisualNovels;
		protected override Func<IDataItem<int>, UserControl> GetTile => i => VNTile.FromListedVN((ListedVN)i);
		protected override NamedFunction DbFunction { get; set; } = new(x => x.VisualNovels, "All", false);
		protected override Func<IEnumerable<IDataItem<int>>, IEnumerable<IDataItem<int>>> Ordering { get; set; } = lvn => lvn.OrderByDescending(i => ((ListedVN)i).ReleaseDate);

		protected override IEnumerable<IDataItem<int>> GetAllWithKeyIn(VisualNovelDatabase db, int[] keys) => db.VisualNovels.WithKeyIn(keys);

		protected override Func<IDataItem<int>, bool> IsBlacklistedFunction => i => ((ListedVN)i).UserVN?.Blacklisted ?? false;
		protected override Func<IDataItem<int>, ListedProducer> GetProducer => i => ((ListedVN)i).Producer;
		public override async Task Initialize()
		{
			MainViewModel.StatusText = "Loading VN Database...";
			await Task.Run(() => StaticHelpers.LocalDatabase = new VisualNovelDatabase(StaticHelpers.DatabaseFile, true));
			OnPropertyChanged(nameof(ProducerList));
			MainViewModel.SetUser(CSettings.UserID);
			MainViewModel.StatusText = "Opening VNDB Connection...";
			await Task.Run(() =>
			{
				StaticHelpers.Conn = new VndbConnection(SetReplyText, MainViewModel.VndbAdvancedAction, AskForNonSsl, ChangeConnectionStatus);
				var password = StaticHelpers.LoadPassword();
				StaticHelpers.Conn.Login(password != null
					? new VndbConnection.LoginCredentials(StaticHelpers.ClientName, StaticHelpers.ClientVersion, CSettings.Username, password)
					: new VndbConnection.LoginCredentials(StaticHelpers.ClientName, StaticHelpers.ClientVersion), false);
			});
			MainViewModel.StatusText = "Loading VN List...";
			await RefreshTiles();
		}

		protected override Func<IDataItem<int>, double?> GetSuggestion => i => ((ListedVN)i).Suggestion?.Score;
		protected override Func<string, Func<IDataItem<int>, bool>> SearchByText => t => i => VisualNovelDatabase.SearchForVN(t)((ListedVN)i);
		public override FiltersViewModel FiltersViewModel { get; } = new(StaticMethods.AllFilters.VnFilters, StaticMethods.AllFilters.VnPermanentFilter);

		public VNTabViewModel(MainWindowViewModel mainWindowViewModel) : base(mainWindowViewModel) { }

		public override int[] GetRelatedTitles(IDataItem<int> item)
		{
			return ((ListedVN)item).GetAllRelations().Select(v => v.ID).Concat(new[] { item.Key }).ToArray();
		}

		public override async Task SortByMyScore()
		{
			Ordering = lvn => lvn.OrderByDescending(i => ((ListedVN)i).UserVN?.Vote).ThenByDescending(i => ((ListedVN)i).ReleaseDate);
			await RefreshTiles();
		}

		public override async Task SortByRating()
		{
			Ordering = lvn => lvn.OrderByDescending(i => ((ListedVN)i).Rating).ThenByDescending(i => ((ListedVN)i).ReleaseDate);
			if(StaticMethods.Settings.GuiSettings.ExcludeLowVotesForRatingSort) Exclude = x => ((ListedVN)x).VoteCount < 10;
			await RefreshTiles();
		}

		public override async Task SortByReleaseDate()
		{
			Ordering = lvn => lvn.OrderByDescending(i => ((ListedVN)i).ReleaseDate);
			await RefreshTiles();
		}

		public override async Task SortByName()
		{
			Ordering = lvn => lvn.OrderBy(i => ((ListedVN)i).Title).ThenByDescending(i => ((ListedVN)i).ReleaseDate);
			await RefreshTiles();
		}

		private static bool AskForNonSsl()
		{
			var messageResult = System.Windows.Forms.MessageBox.Show(@"Connection to VNDB failed, do you wish to try without SSL?",
				@"Connection Failed", System.Windows.Forms.MessageBoxButtons.YesNo);
			return messageResult == System.Windows.Forms.DialogResult.Yes;
		}
	}
}