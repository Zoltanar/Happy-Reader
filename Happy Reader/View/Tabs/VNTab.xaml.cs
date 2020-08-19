using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Tiles;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class VNTab : UserControl
	{
		private MainWindow _mainWindow;
		private readonly ListedVN _viewModel;
		public VNTab(ListedVN vn, UserGame userGame, bool openOnUserGame)
		{
			InitializeComponent();
			_viewModel = vn;
			DataContext = vn;
			var cvnItems = StaticHelpers.LocalDatabase.CharacterVNs.Where(cvn => cvn.VNId == vn.VNID);
			CharacterTiles.ItemsSource = cvnItems.Select(CharacterTile.FromCharacterVN).ToArray();
			if (userGame == null) return;
			var tabItem = new TabItem
			{
				Header = "User Game",
				Name = nameof(UserGameTab),
				Content = new UserGameTab(userGame, true),
				Tag = userGame
			};
			TabControl.Items.Add(tabItem);
			if (openOnUserGame) TabControl.SelectedItem = tabItem;
		}

		private void LoadTags(ListedVN vn)
		{
			var groups = vn.Tags.GroupBy(x => x.Category).OrderBy(g => g.Key.ToString());
			var allInlines = new List<Inline>();
			foreach (var group in groups)
			{
				if (@group.Key == null) continue;
				allInlines.Add(new Run($"{@group.Key}: "));
				foreach (var tag in @group.OrderBy(x => x.Print()))
				{
					var link = new Hyperlink(new Run(tag.Print())) { Tag = tag };
					if (_mainWindow.ViewModel.DatabaseViewModel.SuggestionScorer?.IdTags?.Contains(tag.TagId) ?? false) link.FontWeight = FontWeights.Bold;
					link.PreviewMouseLeftButtonDown += OnTagClick;
					allInlines.Add(link);
				}
			}
			AllTagsControl.ItemsSource = allInlines;
		}

		private async void OnTagClick(object sender, MouseButtonEventArgs e)
		{
			_mainWindow.MainTabControl.SelectedIndex = 3;
			var tag = (DbTag)((Hyperlink)sender).Tag;
			await ((MainWindowViewModel)_mainWindow.DataContext).DatabaseViewModel.ShowTagged(DumpFiles.GetTag(tag.TagId));
		}

		private async void VNPanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			_mainWindow = (MainWindow)Window.GetWindow(this);
			if (_viewModel.Tags?.Count > 0) LoadTags(_viewModel);
			await _viewModel.GetRelationsAnimeScreens();
			ScreensBox.AspectRatio = _viewModel.ScreensObject.Any() ? _viewModel.ScreensObject.Max(x => (double)x.Width / x.Height) : 1;
			ImageBox.MaxHeight = ImageBox.Source.Height;
			if (!_viewModel.RelationsObject.Any())
			{
				RelationsLabel.Visibility = Visibility.Collapsed;
				RelationsCombobox.Visibility = Visibility.Collapsed;
			}
			else RelationsCombobox.SelectedIndex = 0;
			if (!_viewModel.AnimeObject.Any())
			{
				AnimeLabel.Visibility = Visibility.Collapsed;
				AnimeCombobox.Visibility = Visibility.Collapsed;
			}
			else AnimeCombobox.SelectedIndex = 0;
			_viewModel.OnPropertyChanged(null);
		}

		private void ScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			ScrollViewer scrollViewer = (ScrollViewer)sender;
			scrollViewer.CanContentScroll = true;
			if (e.Delta > 0) scrollViewer.LineLeft();
			else scrollViewer.LineRight();
			e.Handled = true;
		}

		private void ID_OnClick(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start($"https://vndb.org/v{_viewModel.VNID}");
		}
		
		private async void ShowVNsForStaff(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			if (!(element?.DataContext is VnStaff vnStaff)) return;
			await _mainWindow.ViewModel.DatabaseViewModel.ShowForStaffWithAlias(vnStaff.AliasID);
			_mainWindow.SelectTab(typeof(DatabaseTab));
		}

		private async void ShowCharactersForStaff(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			if (!(element?.DataContext is VnStaff vnStaff)) return;
			await _mainWindow.ViewModel.CharactersViewModel.ShowForStaffWithAlias(vnStaff.AliasID);
			_mainWindow.SelectTab(typeof(CharactersTab));
		}
	}
}
