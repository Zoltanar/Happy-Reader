using System.Collections.Generic;
using System.Diagnostics;
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
		private bool _loaded;

		private readonly ListedVN _viewModel;
		public VNTab(ListedVN vn, UserGame userGame)
		{
			InitializeComponent();
			_viewModel = vn;
			DataContext = vn;
			if (userGame == null) return;
			var tabItem = new TabItem
			{
				Header = "User Game",
				Name = nameof(UserGameTab),
				Content = new UserGameTab(userGame, true),
				Tag = userGame,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch

			};
			TabControl.Items.Insert(0,tabItem);
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
			_mainWindow.SelectTab(typeof(DatabaseTab));
			var tag = (DbTag)((Hyperlink)sender).Tag;
			await ((MainWindowViewModel)_mainWindow.DataContext).DatabaseViewModel.ShowTagged(DumpFiles.GetTag(tag.TagId));
		}

		private void VNPanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			_mainWindow = (MainWindow)Window.GetWindow(this);
			LoadAliases();
			LoadCharacters();
			if (_viewModel.Tags.Any()) LoadTags(_viewModel);
			LoadScreenshots();
			StaffTab.Visibility = _viewModel.Staff.Any() ? Visibility.Visible : Visibility.Collapsed;
			ImageBox.MaxHeight = ImageBox.Source.Height;
			LoadRelations();
			LoadAnime();
			_loaded = true;
			var firstVisibleTab = TabControl.Items.Cast<FrameworkElement>().FirstOrDefault(t=>t.Visibility != Visibility.Collapsed);
			if(firstVisibleTab != null) TabControl.SelectedItem = firstVisibleTab;
			_viewModel.OnPropertyChanged(null);
		}

		private void LoadAliases()
		{
			var aliasString = _viewModel.Aliases?.Replace(@"\n", ", ");
			if (!string.IsNullOrWhiteSpace(aliasString))
			{
				AliasesTb.Text = aliasString;
				AliasesLabel.Visibility = Visibility.Visible;
				AliasesTb.Visibility = Visibility.Visible;
			}
			else
			{
				AliasesTb.Text = string.Empty;
				AliasesLabel.Visibility = Visibility.Collapsed;
				AliasesTb.Visibility = Visibility.Collapsed;
			}
		}

		private void LoadRelations()
		{
			if (_viewModel.RelationsObject.Length > 0)
			{
				var allRelations = _viewModel.GetAllRelations();
				var titleString = allRelations.Count == 1 ? "1 Relation" : $"{allRelations.Count} Relations";
				var elementList = new List<object> { titleString, "--------------" };
				foreach (var relation in allRelations.OrderBy(c => c.ID))
				{
					var tb = new TextBlock { Text = relation.Print2(), Tag = relation };
					elementList.Add(tb);
				}
				RelationsCombobox.ItemsSource = elementList;
				RelationsCombobox.SelectedIndex = 0;
				RelationsLabel.Visibility = Visibility.Visible;
				RelationsCombobox.Visibility = Visibility.Visible;
			}
			else
			{
				RelationsLabel.Visibility = Visibility.Collapsed;
				RelationsCombobox.Visibility = Visibility.Collapsed;
			}
		}

		private void LoadAnime()
		{
			if (_viewModel.AnimeObject.Length > 0)
			{
				var titleString = $"{_viewModel.AnimeObject.Length} Anime";
				var stringList = new List<string> { titleString, "--------------" };
				stringList.AddRange(_viewModel.AnimeObject.Select(x => x.Print()));
				AnimeCombobox.ItemsSource = stringList;
				AnimeCombobox.SelectedIndex = 0;
				AnimeLabel.Visibility = Visibility.Visible;
				AnimeCombobox.Visibility = Visibility.Visible;
			}
			else
			{
				AnimeLabel.Visibility = Visibility.Collapsed;
				AnimeCombobox.Visibility = Visibility.Collapsed;
			}
		}

		private void LoadScreenshots()
		{
			if (_viewModel.ScreensObject.Length > 0)
			{
				ScreensBox.AspectRatio = _viewModel.ScreensObject.Max(x => (double)x.Width / x.Height);
				ScreenshotsTab.Visibility = Visibility.Visible;
			}
			else
			{
				ScreensBox.AspectRatio = 1;
				ScreenshotsTab.Visibility = Visibility.Collapsed;
			}
		}

		private void LoadCharacters()
		{
			var cvnItems = StaticHelpers.LocalDatabase.CharacterVNs[_viewModel.VNID];
			var characterTiles = cvnItems.Select(CharacterTile.FromCharacterVN).ToArray();
			if (characterTiles.Length > 0)
			{
				CharacterTiles.ItemsSource = characterTiles;
				CharactersTab.Visibility = Visibility.Visible;
			}
			else
			{
				CharactersTab.Visibility = Visibility.Collapsed;
			}
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
			Process.Start($"https://vndb.org/v{_viewModel.VNID}");
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

		private void RelationSelected(object sender, SelectionChangedEventArgs e)
		{
			if (!_loaded) return;
			if (e.AddedItems.Count == 0) return;
			var relationElement = e.AddedItems.OfType<TextBlock>().FirstOrDefault();
			if (!(relationElement?.Tag is RelationsItem relation)) return;
			var vn = StaticHelpers.LocalDatabase.VisualNovels[relation.ID];
			if (vn != null) _mainWindow.OpenVNPanel(vn);
		}
	}
}
