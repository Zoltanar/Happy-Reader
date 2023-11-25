using System;
using System.Collections.Generic;
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
		public override Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> GetAll => db => db.VisualNovels;
		protected override Func<IDataItem<int>, UserControl> GetTile => i => VNTile.FromListedVN((ListedVN)i);
		protected override NamedFunction DbFunction { get; set; } = new(new CustomFilter("All"));
		public override IEnumerable<IDataItem<int>> GetAllWithKeyIn(VisualNovelDatabase db, int[] keys) => db.VisualNovels.WithKeyIn(keys);
		protected override Func<IDataItem<int>, ListedVN> GetVisualNovel => i => (ListedVN)i;
		protected override Func<IDataItem<int>, double?> GetSuggestion => i => ((ListedVN)i).Suggestion?.Score;
		protected override Func<IDataItem<int>, string> GetName => i => ((ListedVN)i).Title;

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
            SelectedFilterIndex = 0;
            //await RefreshTiles();
		}

		public override FiltersViewModel FiltersViewModel { get; }

		public VNTabViewModel(MainWindowViewModel mainWindowViewModel) : base(mainWindowViewModel)
		{
			FiltersViewModel = new(StaticMethods.AllFilters.VnFilters, StaticMethods.AllFilters.VnPermanentFilter, this);
		}
		
		private static bool AskForNonSsl()
		{
			var messageResult = System.Windows.Forms.MessageBox.Show(@"Connection to VNDB failed, do you wish to try without SSL?",
				@"Connection Failed", System.Windows.Forms.MessageBoxButtons.YesNo);
			return messageResult == System.Windows.Forms.DialogResult.Yes;
		}
	}
}