using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.View
{
	public partial class VnMenuItem : ItemsControl
	{
		private static readonly Regex ExcludedSearchCharacters = new(@"[-－‐～]");

		private ListedVN VN => (ListedVN)DataContext;
		public VnMenuItem(ListedVN vn)
		{
			InitializeComponent();
			DataContext = vn;
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
			var itemIndex = Items.IndexOf(ReleaseLinkItem);
			foreach (var link in StaticMethods.Settings.GuiSettings.PageLinks ?? Array.Empty<PageLink>().AsEnumerable())
			{
				var menuItem = new MenuItem
				{
					Header = $"Search on {link.Label}",
					Tag = link
				};
				menuItem.Click += BrowseToExtraPage;
				this.Items.Insert(++itemIndex, menuItem);
			}
		}

		public void BrowseToVndbPage(object sender, RoutedEventArgs e) => Process.Start($"http://vndb.org/v{VN.VNID}/");

		private void BrowseToReleasePage(object sender, RoutedEventArgs e)
		{
			if (!Uri.IsWellFormedUriString(VN.ReleaseLink, UriKind.Absolute)) throw new InvalidOperationException($"'{VN.ReleaseLink}' is not a well formed URI.");
			Process.Start(VN.ReleaseLink);
		}

		private void BrowseToExtraPage(object sender, RoutedEventArgs e)
		{
			var pageLink = (PageLink)((MenuItem)sender).Tag;
			var title = pageLink.UseRomaji
				? !string.IsNullOrWhiteSpace(VN.Title) ? VN.Title : VN.KanjiTitle
				: !string.IsNullOrWhiteSpace(VN.KanjiTitle) ? VN.KanjiTitle : VN.Title;
			var titleFixed = ExcludedSearchCharacters.Replace(title, string.Empty);
			var link = pageLink.Link.Replace("%s", titleFixed).Replace(" ", "%20");
			if (!Uri.IsWellFormedUriString(link, UriKind.Absolute)) throw new InvalidOperationException($"'{link}' is not a well formed URI.");
			Process.Start(link);
		}

		public void ContextMenuOpened(bool fromUserGame)
		{
			ReleaseLinkItem.IsEnabled = !string.IsNullOrWhiteSpace(VN.ReleaseLink);
			OriginalTitleItem.IsEnabled = !string.IsNullOrWhiteSpace(VN.KanjiTitle);
			TranslateTitleItem.IsEnabled = !string.IsNullOrWhiteSpace(VN.KanjiTitle);
			//clearing previous
			foreach (MenuItem item in Labels.Items) item.IsChecked = false;
			foreach (MenuItem item in VoteMenu.Items) item.IsChecked = false;
			ShowByStaffItem.Items.Clear();
			//set new
			Labels.IsChecked = VN.UserVN?.Labels.Any(l => l != UserVN.LabelKind.Voted) ?? false;
			VoteMenu.IsChecked = VN.UserVN?.Vote > 0;
			foreach (var label in VN.UserVN?.Labels.AsEnumerable() ?? Array.Empty<UserVN.LabelKind>())
			{
				var labelItem = Labels.Items.Cast<MenuItem>().FirstOrDefault(menuItems => ((string)menuItems.Tag).EndsWith(label.ToString()));
				if (labelItem != null) labelItem.IsChecked = true;
			}
			if (VN.UserVN?.Vote != null)
			{
				if (VN.UserVN.Vote % 10 == 0)
				{
					var vote = (int)Math.Round(VN.UserVN.Vote.Value / 10d);
					((MenuItem)VoteMenu.Items[vote]).IsChecked = true;
				}
				else VoteMenu.Items.Cast<MenuItem>().Last().IsChecked = true;
			}
			else ((MenuItem)VoteMenu.Items[0]).IsChecked = true;
			if (VN.Producer?.IsFavorited ?? false)
			{
				AddProducerToFavoritesItem.IsEnabled = false;
				AddProducerToFavoritesItem.ToolTip = @"Already in list.";
			}
			if (VN.RelationsObject.Length == 0)
			{
				ShowRelatedTitlesItem.IsEnabled = false;
				ShowRelatedTitlesItem.ToolTip = @"There are no related titles.";
			}
			if (!VN.Staff.Any())
			{
				ShowByStaffItem.IsEnabled = false;
				ShowByStaffItem.ToolTip = @"There are no staff credits.";
			}
			else
			{
				var grouped = VN.Staff.GroupBy(s => s.RoleDetail).OrderBy(g => g.Key).ToList();
				foreach (var group in grouped)
				{
					var menuItem = new MenuItem() { Header = group.Key, Tag = group };
					menuItem.Click += ShowByStaff;
					ShowByStaffItem.Items.Add(menuItem);
				}
				var artScenarioStaff = grouped.Where(g => g.Key.Equals("Art") || g.Key.Equals("Scenario")).ToList();
				if (artScenarioStaff.Count == 2)
				{
					var menuItem = new MenuItem
					{
						Header = "Art & Scenario ",
						ToolTip = "Titles with at least one of the same Art staff and one of the same Scenario staff.",
						Tag = artScenarioStaff
					};
					menuItem.Click += ShowByStaffArtScenario;
					ShowByStaffItem.Items.Add(menuItem);
				}
			}
			if (fromUserGame)
			{
				LaunchSeparator.Visibility = Visibility.Collapsed;
				LaunchItem.Visibility = Visibility.Collapsed;
				return;
			}
			LaunchSeparator.Visibility = Visibility.Visible;
			LaunchItem.Visibility = Visibility.Visible;
			if (VN.IsOwned == OwnedStatus.CurrentlyOwned)
			{
				SetLaunchItem();
			}
			else
			{
				LaunchItem.Tag = null;
				LaunchItem.Click -= Launch;
				LaunchItem.IsEnabled = false;
			}
		}

		private void SetLaunchItem()
		{
			LaunchItem.IsEnabled = true;
			var games = StaticMethods.Data.UserGames.Where(ug => ug.VNID == VN.VNID && ug.FileExists).ToList();
			if (games.Count == 1)
			{
				LaunchItem.Tag = games.First();
				LaunchItem.Click += Launch;
			}
			else
			{
				foreach (var game in games)
				{
					var item = new MenuItem
					{
						Header = game.DisplayName,
						Tag = game
					};
					item.Click += Launch;
					LaunchItem.Items.Add(item);
				}

				LaunchItem.Tag = null;
				LaunchItem.Click -= Launch;
			}
		}

		private void ShowByStaff(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var staffType = menuItem.Header;
			var group = (IEnumerable<VnStaff>)menuItem.Tag;
			var staff = group.Select(s => s.AliasID).ToList();
			var databaseViewModel = StaticMethods.MainWindow.ViewModel.DatabaseViewModel;
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			if (staff.Count == 1) databaseViewModel.ShowForStaffWithAlias(staff.First());
			else databaseViewModel.ShowForStaffWithAlias($"{staffType} ({staff.Count}) for {StaticHelpers.TruncateString(VN.Title, 15)}", staff);
		}

		private void ShowByStaffArtScenario(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var filter = new CustomFilter
			{
				Name = $"Art/Scenario: {StaticHelpers.TruncateString(VN.Title, 15)}"
			};
			var staffGroups = (IEnumerable<IGrouping<string,VnStaff>>)menuItem.Tag;
			foreach (var group in staffGroups)
			{
				foreach (var staff in group)
				{

					filter.OrFilters.Add(new GeneralFilter(GeneralFilterType.Staff, staff.AliasID));
				}
				filter.SaveOrGroup();
			}
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.SelectedFilter = filter;
		}

		private async void ChangeLabel(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var remove = menuItem.IsChecked;
			List<UserVN.LabelKind> labels = new List<UserVN.LabelKind>();
			if (!(menuItem.Tag is string tag) || string.IsNullOrWhiteSpace(tag)) throw new InvalidOperationException("Did not find tag for menu item.");
			try
			{
				labels.AddRange(tag.Split(',').Select(l => (UserVN.LabelKind)Enum.Parse(typeof(UserVN.LabelKind), l)));
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Failed to parse labels", ex);
			}
			HashSet<UserVN.LabelKind> labelsToSet;
			if (remove) labelsToSet = VN.UserVN.Labels.Except(labels).ToHashSet();
			else
			{
				labelsToSet = new HashSet<UserVN.LabelKind>();
				if (VN.UserVN?.Labels?.Contains(UserVN.LabelKind.Voted) ?? false) labelsToSet.Add(UserVN.LabelKind.Voted);
				labelsToSet.UnionWith(labels);
			}
			var success = await StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ChangeVNStatus(VN, labelsToSet);
			if (success)
			{
				VN.OnPropertyChanged(null);
				VN.Producer?.OnPropertyChanged(null);
				var vnCharacters = StaticHelpers.LocalDatabase.CharacterVNs[VN.VNID];
				var characters = StaticHelpers.LocalDatabase.Characters.WithKeyIn(vnCharacters.Select(c=>c.CharacterId).ToList());
				foreach(var character in characters) character.OnPropertyChanged(null);
			}
		}

		private async void ChangeVote(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var remove = menuItem.IsChecked;
			var header = menuItem.Header.ToString();
			var voteValue = remove ? null : header == "None" ? (int?)null : int.Parse(header);
			var success = await StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ChangeVote(VN, voteValue);
			if (success) VN.OnPropertyChanged(null);
		}

		private static bool _showingInputControl;

		public void ChangePreciseNumber(object sender, RoutedEventArgs e)
		{
			if (_showingInputControl) return;
			var selectedTab = (TabItem)StaticMethods.MainWindow.MainTabControl.SelectedItem;
			var grid = (Grid)((UserControl)selectedTab.Content).Content;
			var gridChildren = grid.Children.Cast<UIElement>().ToArray();
			_showingInputControl = true;
			var inputWindow = new InputControl($"Enter your vote for {VN.Title}:", Callback)
			{
				InputLabel = StaticMethods.Settings.GuiSettings.UseDecimalVoteScores ? "Enter vote value from 1 to 10" : "Enter vote value from 10 to 100",
				Filter = s => (StaticMethods.Settings.GuiSettings.UseDecimalVoteScores && double.TryParse(s, out var dVote) && dVote >= 1 && dVote <= 10)
				              || int.TryParse(s, out var vote) && vote >= 10 && vote <= 100
			};
			grid.Children.Clear();
			grid.Children.Add(inputWindow);

			async Task Callback(bool success, string inputText)
			{
				try
				{
					await ChangeVoteCallback(success, VN, inputText);
					grid.Children.Clear();
					foreach (var gridChild in gridChildren)
					{
						grid.Children.Add(gridChild);
					}
				}
				finally
				{
					_showingInputControl = false;
				}
			}
		}

		private async Task ChangeVoteCallback(bool successful, ListedVN vn, string inputText)
		{
			if (!successful) return;
			var voteValue = StaticMethods.Settings.GuiSettings.UseDecimalVoteScores ? (int)(double.Parse(inputText) * 10) : int.Parse(inputText);
			var success = await StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ChangeVote(vn, voteValue);
			if (success) vn.OnPropertyChanged(null);
		}

		private void ShowRelatedTitles(object sender, RoutedEventArgs e)
		{
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowRelatedTitles(VN);
		}

		private void ShowTitlesByProducer(object sender, RoutedEventArgs e)
		{
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowForProducer(VN.Producer);
		}

		private void CopyTitle(object sender, RoutedEventArgs e) => Clipboard.SetText(VN.Title);

		private void CopyOriginalTitle(object sender, RoutedEventArgs e) => Clipboard.SetText(VN.KanjiTitle);

		public void TransferItems(ItemsControl parent)
		{
			foreach (var item in Items.Cast<FrameworkElement>().ToList())
			{
				Items.Remove(item);
				parent.Items.Add(item);
			}
		}

		private void ImportNames(object sender, RoutedEventArgs e)
		{
			var cvns = StaticHelpers.LocalDatabase.CharacterVNs[VN.VNID].ToList();
			var characterEntries = cvns.SelectMany(GetEntriesFromCharacter).ToList();
			var newEntries = characterEntries.Except(StaticMethods.Data.Entries, Entry.ClashComparer).OrderBy(i => i.Input).ToList();
			if (newEntries.Count == 0)
			{
				StaticMethods.MainWindow.ViewModel.NotificationEvent(this, "No new names to import.", "Import Names");
				return;
			}
			StaticMethods.MainWindow.CreateAddEntriesTab(newEntries);
		}

		private void Launch(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menu || menu.Tag is not UserGame game) return;
			StaticMethods.MainWindow.ViewModel.HookUserGame(game,null,null,false);
		}

		private static List<Entry> GetEntriesFromCharacter(CharacterVN cvn)
		{
			var entries = new List<Entry>();
			var character = StaticHelpers.LocalDatabase.Characters[cvn.CharacterId];
			if (string.IsNullOrWhiteSpace(character.Name) || string.IsNullOrWhiteSpace(character.Original)) return entries;
			var outputParts = character.Name.Split(' ', '・');
			var inputParts = character.Original.Split(' ', '・');
            var roleString = character.Gender == "f" ? "m.f" : "m";
			if (outputParts.Length != inputParts.Length)
			{
				var entry = new Entry
				{
					RoleString = roleString,
					Input = character.Original,
					Output = character.Name,
					SeriesSpecific = true,
					Type = EntryType.Name
				};
				entry.SetGameId(cvn.VNId, false);
				entries.Add(entry);
			}
			else
			{
				for (int i = 0; i < outputParts.Length; i++)
				{
					var entry = new Entry
					{
						RoleString = roleString,
						Input = inputParts[i],
						Output = outputParts[i],
						SeriesSpecific = true,
						Type = EntryType.Name
					};
					entry.SetGameId(cvn.VNId, false);
					entries.Add(entry);
				}
			}
			return entries;
		}

		private void AddProducerToFavoritesItem_OnClick(object sender, RoutedEventArgs e)
		{
			Debug.Assert(VN.ProducerID != null, "VN.ProducerID != null");
			StaticHelpers.LocalDatabase.UserProducers.Add(new UserListedProducer
			{
				ListedProducer_Id = VN.ProducerID.Value,
				User_Id = StaticHelpers.LocalDatabase.CurrentUser.Id
			}, true, true);
			VN.Producer.SetFavoriteProducerData(StaticHelpers.LocalDatabase);
		}

		private void TranslateOriginalTitle(object sender, RoutedEventArgs e)
		{
			string message;
			try
			{
				var translation = TranslationEngine.Translator.Instance.Translate(
					StaticMethods.MainWindow.ViewModel.User, new EntryGame(VN.VNID, false, false), VN.KanjiTitle, false, false);
				message = translation.Output;
			}
			catch (Exception ex)
			{
				message = ex.Message;
			}
			StaticMethods.MainWindow.ViewModel.NotificationEvent(this, message, $"Translated Title for {VN.Title}");
		}
	}
}
