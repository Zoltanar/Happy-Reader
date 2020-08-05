using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	/// <summary>
	/// Interaction logic for VNTab.xaml
	/// </summary>
	public partial class DatabaseTab : UserControl
	{
		public VNTabViewModel ViewModel { get; private set; }
		private MainWindow _mainWindow;
		private bool _userInteractionHistory;
		private bool _loaded;
		public DatabaseTab() => InitializeComponent();

		private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (!(e.VerticalChange > 0)) return;
			var loc = e.VerticalOffset + e.ViewportHeight * 2;
			if (loc < e.ExtentHeight) return;
			ViewModel.AddListedVNPage();
		}

		private async void ShowAll(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowAll();
		}

		private async void SearchForVN(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			await ViewModel.SearchForVN(((TextBox)sender).Text);
		}

		private void VNTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			ViewModel = (VNTabViewModel)DataContext;
			_mainWindow = (MainWindow)Window.GetWindow(this);
			ViewModel.OnPropertyChanged(nameof(ViewModel.ProducerList));
			_loaded = true;
		}

		private void VNTileDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var item = VisualNovelItems.SelectedItem as Tiles.VNTile;
			var vn = (ListedVN)item?.DataContext;
			if (vn == null) return;
			_mainWindow.OpenVNPanel(vn);
		}

		private async void ShowSuggested(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowSuggested();
		}

		private async void SortByRecommended(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByRecommended();
		}

		private async void SortByMyScore(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByMyScore();
		}

		private async void SortByRelease(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByReleaseDate();
		}

		private async void SortByRating(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByRating();
		}

		private async void SortByTitle(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByTitle();
		}

		private bool ProducerBoxFilter(string search, object value)
		{
			var lowerSearch = search.ToLower();
			var producer = (ListedProducer)value;
			return producer.Name.ToLower().Contains(lowerSearch);
		}

		private async void SelectProducer(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0) return;
			if (!(e.AddedItems[0] is ListedProducer producer)) return;
			await ViewModel.ShowForProducer(producer);
		}

		private async void BrowseHistory(object sender, SelectionChangedEventArgs e)
		{
			if (!_userInteractionHistory || e.AddedItems.Count == 0) return;
			var item = e.AddedItems.Cast<NamedFunction>().First();
			if (item.Selected)
			{
				return;
			}
			await ViewModel.BrowseHistory(item);
		}

		private void AllowUserInteraction(object sender, EventArgs e) => _userInteractionHistory = true;

		private void StopUserInteraction(object sender, EventArgs e) => _userInteractionHistory = false;
	}
}
