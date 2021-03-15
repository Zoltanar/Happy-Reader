using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Tiles;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class DatabaseTab : UserControl
	{
		public DatabaseViewModelBase ViewModel { get; private set; }
		private bool _userInteractionHistory;
		private bool _loaded;

		public DatabaseTab() => InitializeComponent();

		private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (!(e.VerticalChange > 0)) return;
			var loc = e.VerticalOffset + e.ViewportHeight * 2;
			if (loc < e.ExtentHeight) return;
			ViewModel.AddPage();
		}

		private async void ShowAll(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowAll();
		}

		private async void SearchForVN(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			await ViewModel.SearchForItem(((TextBox)sender).Text);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			ViewModel = (DatabaseViewModelBase)DataContext;
			_loaded = true;
		}

		private async void ShowSuggested(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowSuggested();
		}

		private async void SortByID(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByID();
		}

		private async void SortByRecommended(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortBySuggestion();
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

		private async void SortByName(object sender, RoutedEventArgs e)
		{
			if (ViewModel != null) await ViewModel.SortByName();
		}

		private bool ProducerBoxFilter(string input, object item)
		{
			//Short input is not filtered to prevent excessive loading times
			if (input.Length <= 2) return false;
			var producer = (ListedProducer)item;
			return producer.Name.ToLowerInvariant().Contains(input.ToLowerInvariant());
		}

		private void SelectProducerOnKey(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			ProducerItemSelected((AutoCompleteBox)sender);
		}

		private void ProducerItemClicked(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left) return;
			ProducerItemSelected((AutoCompleteBox)sender);
		}

		private async void ProducerItemSelected(AutoCompleteBox textBox)
		{
			if (textBox.SelectedItem is ListedProducer producer) await ViewModel.ShowForProducer(producer);
			else if (!string.IsNullOrWhiteSpace(textBox.Text)) await ViewModel.ShowForProducer(textBox.Text);
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

		private void ShowFilters(object sender, RoutedEventArgs e)
		{
			FiltersPane.Visibility = FiltersPane.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
		}

		private void VNTileMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Middle) OpenVNPanel(e, (FrameworkElement)sender, false);
		}

		private void VNTileDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left) OpenVNPanel(e, (FrameworkElement)sender, true);
		}

		private void OpenVNPanel(MouseEventArgs e, FrameworkElement element, bool switchToTab)
		{
			var hitTestResult = VisualTreeHelper.HitTest(element, e.GetPosition(element));
			var vnTile = hitTestResult.VisualHit.FindParent<VNTile>();
			var item = vnTile?.DataContext;
			var vn = item switch
			{
				ListedVN iVn => iVn,
				CharacterItem ch => StaticHelpers.LocalDatabase.VisualNovels[ch.CharacterVN.VNId],
				_ => null
			};
			if (vn == null) return;
			StaticMethods.MainWindow.OpenVNPanel(vn, switchToTab);
		}
	}
}
