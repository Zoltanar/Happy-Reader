using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class CharactersTab : UserControl
	{
		public CharactersTabViewModel ViewModel { get; private set; }

		private MainWindow _mainWindow;
		public CharactersTab() => InitializeComponent();

		private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (!(e.VerticalChange > 0)) return;
			var loc = e.VerticalOffset + e.ViewportHeight * 2;
			if (loc < e.ExtentHeight) return;
			ViewModel.AddPage();
			//((ScrollViewer)e.OriginalSource).ScrollToVerticalOffset(loc);
		}

		private async void ShowAll(object sender, RoutedEventArgs e)
		{
			await ViewModel.RefreshCharacterTiles(true);
		}

		private async void SearchForVN(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			await ViewModel.Search(((TextBox)sender).Text);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			ViewModel = (CharactersTabViewModel)DataContext;
			_mainWindow = (MainWindow)Window.GetWindow(this);
			var scrollViewer = this.GetDescendantByType<ScrollViewer>();
			ViewModel.ScrollToTop = () => scrollViewer.ScrollToTop();
			ViewModel.OnPropertyChanged(nameof(ViewModel.ProducerList));
		}

		private void TileDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var item = CharacterItems.SelectedItem as Tiles.CharacterTile;
			var ch = (CharacterItem)item?.DataContext;
			if (ch?.CharacterVN == null) return;
			_mainWindow.OpenVNPanel(StaticHelpers.LocalDatabase.VisualNovels[ch.CharacterVN.VNId]);
		}
		
		private async void ShowSuggested(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowSuggested();
		}

		private async void ShowFiltered(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowFiltered();
		}

		private async void SortByRecommended(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByRecommended();
		}

		private async void SortByID(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByID();
		}

		private async void SortByReleaseDate(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByReleaseDate();
		}

		private async void SelectProducer(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0) return;
			if (!(e.AddedItems[0] is ListedProducer producer)) return;
			await ViewModel.ShowForProducer(producer);
		}

		private bool ProducerBoxFilter(string search, object value)
		{
			var lowerSearch = search.ToLower();
			var producer = (ListedProducer)value;
			return producer.Name.ToLower().Contains(lowerSearch);
		}
	}
}
