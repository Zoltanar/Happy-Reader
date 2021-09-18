using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
		private bool _loaded;

		public readonly ListedVN ViewModel;
		public readonly IList<UserGame> UserGames;

		public VNTab(ListedVN vn, IList<UserGame> userGames)
		{
			InitializeComponent();
			ViewModel = vn;
			DataContext = vn;
			TileBox.Children.Add(VNTile.FromListedVN(vn));
			UserGames = userGames;
			var differentDisplayNames = userGames.Where(ug => !string.IsNullOrWhiteSpace(ug.UserDefinedName)).Select(ug => ug.UserDefinedName).Distinct().Count() == userGames.Count;
			var differentFileNames = userGames.Select(ug => Path.GetFileName(ug.FilePath)).Distinct().Count() == userGames.Count;
			int index = 0;
			foreach (var userGame in userGames)
			{
				index++;
				var headerName = userGames.Count == 1 ? "User Game" :
					differentDisplayNames ? $"UG: {userGame.UserDefinedName}" :
					differentFileNames ? $"UG: {Path.GetFileName(userGame.FilePath)}" : $"User Game {index}";
				var header = StaticMethods.GetTabHeader(headerName, new Binding(nameof(UserGame.DisplayName)) { Source = userGame }, userGame, null);
				var tabItem = new TabItem
				{
					Header = header,
					Name = nameof(UserGameTab),
					Content = new UserGameTab(userGame, true),
					Tag = userGame,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};
				TabControl.Items.Insert(0, tabItem);
			}
		}

		private void LoadTags(ListedVN vn)
		{
			var groups = vn.Tags.GroupBy(x => x.Category).OrderBy(g => g.Key.ToString());
			var allInlines = new List<Inline>();
			foreach (var group in groups)
			{
				if (@group.Key == null || !StaticMethods.Settings.GuiSettings.ShowTags(group.Key.Value)) continue;
				allInlines.Add(new Run($"{@group.Key}: "));
				foreach (var tag in @group.OrderBy(x => x.Print()))
				{
					var link = new Hyperlink(new Run(tag.Print())) { Tag = tag };
					if (StaticMethods.MainWindow.ViewModel.DatabaseViewModel.SuggestionScorer?.IdTags?.Contains(tag.TagId) ?? false) link.FontWeight = FontWeights.Bold;
					link.PreviewMouseLeftButtonDown += OnTagClick;
					allInlines.Add(link);
				}
			}
			AllTagsControl.ItemsSource = allInlines;
		}

		private void OnTagClick(object sender, MouseButtonEventArgs e)
		{
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			var tag = (DbTag)((Hyperlink)sender).Tag;
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowTagged(DumpFiles.GetTag(tag.TagId));
		}

		private void VNPanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			if (string.IsNullOrWhiteSpace(ViewModel.Description)) DescriptionRow.Height = new GridLength(0);
			LoadAliases();
			LoadCharacters();
			if (ViewModel.Tags.Any()) LoadTags(ViewModel);
			LoadScreenshots();
			StaffTab.Visibility = ViewModel.Staff.Any() ? Visibility.Visible : Visibility.Collapsed;
			LoadRelations();
			LoadAnime();
			_loaded = true;
			var firstVisibleTab = TabControl.Items.Cast<FrameworkElement>().FirstOrDefault(t => t.Visibility != Visibility.Collapsed);
			if (firstVisibleTab != null) TabControl.SelectedItem = firstVisibleTab;
			ViewModel.OnPropertyChanged(null);
		}

		private void LoadAliases()
		{
			var aliasString = ViewModel.Aliases?.Replace(@"\n", ", ");
			if (!string.IsNullOrWhiteSpace(aliasString)) AliasesTb.Text = aliasString;
			else AliasRow.Height = new GridLength(0);
		}

		private void LoadRelations()
		{
			if (ViewModel.RelationsObject.Length <= 0)
			{
				RelationsRow.Height = new GridLength(0);
				return;
			}
			var allRelations = ViewModel.GetAllRelations();
			var titleString = allRelations.Count == 1 ? "1 Relation" : $"{allRelations.Count} Relations";
			var elementList = new List<object> {titleString, "--------------"};
			foreach (var relation in allRelations.OrderBy(c => c.ID))
			{
				var tb = new TextBlock {Text = relation.Print(), Tag = relation};
				elementList.Add(tb);
			}
			RelationsCombobox.ItemsSource = elementList;
			RelationsCombobox.SelectedIndex = 0;
		}

		private void LoadAnime()
		{
			if (ViewModel.AnimeObject.Length <= 0)
			{
				AnimeRow.Height = new GridLength(0);
				return;
			}
			var titleString = $"{ViewModel.AnimeObject.Length} Anime";
			var stringList = new List<string> {titleString, "--------------"};
			stringList.AddRange(ViewModel.AnimeObject.Select(x => x.Print()));
			AnimeCombobox.ItemsSource = stringList;
			AnimeCombobox.SelectedIndex = 0;
		}

		private void LoadScreenshots()
		{
			if (ViewModel.ScreensObject.Length > 0)
			{
				ScreensBox.AspectRatio = ViewModel.ScreensObject.Max(x => (double)x.Width / x.Height);
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
			var cvnItems = StaticHelpers.LocalDatabase.CharacterVNs[ViewModel.VNID];
			var characterTiles = cvnItems.OrderBy(cvn=>cvn.Role).Select(CharacterTile.FromCharacterVN).ToArray();
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

		private void ShowVNsForStaff(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			if (element?.DataContext is not VnStaff vnStaff) return;
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowForStaffWithAlias(vnStaff.AliasID);
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
		}

		private void ShowCharactersForStaff(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			if (!(element?.DataContext is VnStaff vnStaff)) return;
			StaticMethods.MainWindow.ViewModel.CharactersViewModel.ShowForStaffWithAlias(vnStaff.AliasID);
			StaticMethods.MainWindow.SelectTab(typeof(CharactersTabViewModel));
		}

		private void RelationSelected(object sender, SelectionChangedEventArgs e)
		{
			if (!_loaded) return;
			if (e.AddedItems.Count == 0) return;
			var relationElement = e.AddedItems.OfType<TextBlock>().FirstOrDefault();
			if (relationElement?.Tag is not RelationsItem relation) return;
			var vn = StaticHelpers.LocalDatabase.VisualNovels[relation.ID];
			if (vn != null) StaticMethods.MainWindow.OpenVNPanel(vn);
		}
	}
}
