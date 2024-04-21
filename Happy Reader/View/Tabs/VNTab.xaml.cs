using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Converters;
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
			foreach (var userGame in userGames)
            {
				var userGameTab = new UserGameTab(userGame, true);
                var background = StaticMethods.GetTabHeaderBackgroundAndSaveTab(userGameTab, null);
                var tabItem = new TabItem
				{
					Name = nameof(UserGameTab),
					Content = userGameTab,
					Tag = userGame,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					Background = background
				};
                var headerProperty = new Binding(nameof(UserGame.DisplayName)) { Source = userGame };
                tabItem.SetBinding(HeaderedContentControl.HeaderProperty, headerProperty);
                TabControl.Items.Insert(0, tabItem);
			}
		}

		private void LoadTags(ListedVN vn)
		{
			var groups = vn.Tags(StaticHelpers.LocalDatabase).GroupBy(x => x.Category).OrderBy(g => g.Key.ToString());
			var allInlines = new List<Inline>();
			foreach (var group in groups)
			{
				if (@group.Key == null || !StaticMethods.Settings.GuiSettings.ShowTags(group.Key.Value)) continue;
				allInlines.Add(new Run($"{@group.Key}: "));
				foreach (var tag in @group.OrderBy(x => x.Print()))
				{
					var writtenTag = DumpFiles.GetTag(tag.TagId);
					var tooltip = writtenTag != null ? TitleDescriptionConverter.Instance.Convert(writtenTag.Description, typeof(string), null, CultureInfo.CurrentCulture) : null;
					var link = new Hyperlink(new Run(tag.Print())) { Tag = writtenTag, ToolTip = tooltip };
					if (StaticMethods.MainWindow.ViewModel.DatabaseViewModel.SuggestionScorer?.IdTags?.Contains(tag.TagId) ?? false) link.FontWeight = FontWeights.Bold;
					if(writtenTag != null) link.PreviewMouseLeftButtonDown += OnTagClick;
					allInlines.Add(link);
				}
			}
			AllTagsControl.ItemsSource = allInlines;
		}

		private void OnTagClick(object sender, MouseButtonEventArgs e)
		{
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			var tag = (DumpFiles.WrittenTag)((Hyperlink)sender).Tag;
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowTagged(tag);
		}

		private void VNPanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			if (string.IsNullOrWhiteSpace(ViewModel.Description)) DescriptionRow.Height = new GridLength(0);
			LoadAliases();
			LoadCharacters();
			if (ViewModel.Tags(StaticHelpers.LocalDatabase).Any()) LoadTags(ViewModel);
			LoadScreenshots();
			StaffTab.Visibility = ViewModel.Staff.Any() ? Visibility.Visible : Visibility.Collapsed;
			LoadRelations();
			LoadAnime();
			LoadNotes();
			_loaded = true;
			var firstVisibleTab = TabControl.Items.Cast<FrameworkElement>().FirstOrDefault(t => t.Visibility != Visibility.Collapsed);
			if (firstVisibleTab != null) TabControl.SelectedItem = firstVisibleTab;
			ViewModel.OnPropertyChanged(null);
		}

		private void LoadAliases()
		{
			var regex = new Regex(@"\\+n+");
			var aliasString = string.IsNullOrWhiteSpace(ViewModel.Aliases) ? null : regex.Replace(ViewModel.Aliases, ", ");
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
			var elementList = new List<object> { titleString, "--------------" };
			foreach (var relation in allRelations.OrderBy(c => c.ID))
			{
				var tb = new TextBlock { Text = relation.Print(), Tag = relation };
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
			var stringList = new List<string> { titleString, "--------------" };
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
			var characterTiles = cvnItems.OrderBy(cvn => cvn.Role).Select(CharacterTile.FromCharacterVN).ToArray();
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

		private void LoadNotes()
		{
			NotesBox.Text = ViewModel?.UserVN?.ULNote ?? string.Empty;
		}

		private async void SaveNotesLostFocus(object sender, RoutedEventArgs e)
		{
			if (NotesBox.Text.Equals(ViewModel?.UserVN?.ULNote ?? string.Empty)) return;
			await SaveNotes();
		}


		private async void SaveNotesKey(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			await SaveNotes();
		}

		private async Task SaveNotes()
		{
			if (NotesBox.Text.Equals(ViewModel?.UserVN?.ULNote ?? string.Empty)) return;
			await StaticHelpers.Conn.ChangeVNNote(ViewModel, NotesBox.Text);
		}
	}
}
