using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.View
{
	public partial class VnMenuItem : ItemsControl
	{
		private static readonly Regex MinusRegex = new Regex(@"[-－]");

		[NotNull]
		private MainWindow MainWindow => (MainWindow)Application.Current.MainWindow ?? throw new InvalidOperationException();

		private MainWindowViewModel MainViewModel => MainWindow.ViewModel;
		private VNTabViewModel ViewModel => MainViewModel.DatabaseViewModel;
		private ListedVN VN => (ListedVN)DataContext;
		public VnMenuItem(ListedVN vn)
		{
			InitializeComponent();
			DataContext = vn;
			var itemIndex = Items.IndexOf(ReleaseLinkItem);
			foreach (var link in StaticMethods.GuiSettings.PageLinks ?? Array.Empty<PageLink>().AsEnumerable())
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

		private void BrowseToVndbPage(object sender, RoutedEventArgs e) => Process.Start($"http://vndb.org/v{VN.VNID}/");

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
			//remove minus so search includes term
			var titleFixed = MinusRegex.Replace(title, string.Empty);
			var link = pageLink.Link.Replace("%s", titleFixed).Replace(" ", "%20");
			if (!Uri.IsWellFormedUriString(link, UriKind.Absolute)) throw new InvalidOperationException($"'{link}' is not a well formed URI.");
			Process.Start(link);
		}

		public void ContextMenuOpened()
		{
			ReleaseLinkItem.IsEnabled = !string.IsNullOrWhiteSpace(VN.ReleaseLink);
			OriginalTitleItem.IsEnabled = !string.IsNullOrWhiteSpace(VN.KanjiTitle);
			TranslateTitleItem.IsEnabled = !string.IsNullOrWhiteSpace(VN.KanjiTitle);
			//clearing previous
			foreach (MenuItem item in Labels.Items) item.IsChecked = false;
			foreach (MenuItem item in VoteMenu.Items) item.IsChecked = false;
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
			var success = await ViewModel.ChangeVNStatus(VN, labelsToSet);
			if (success)
			{
				VN.OnPropertyChanged(null);
				VN.Producer.OnPropertyChanged(null);
			}
		}

		private async void ChangeVote(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var remove = menuItem.IsChecked;
			var header = menuItem.Header.ToString();
			var voteValue = remove ? null : header == "None" ? (int?)null : int.Parse(header);
			var success = await ViewModel.ChangeVote(VN, voteValue);
			if (success) VN.OnPropertyChanged(null);
		}

		private async void ChangePreciseNumber(object sender, RoutedEventArgs e)
		{
			var inputWindow = new InputWindow
			{
				Title = $"{StaticHelpers.ClientName} - Enter Visual Novel Vote",
				InputLabel = "Enter vote value from 10 to 100",
				Filter = s => int.TryParse(s, out var vote) && vote >= 10 && vote <= 100,
			};
			var result = inputWindow.ShowDialog();
			if (result == true)
			{
				var voteValue = int.Parse(inputWindow.InputText);
				var success = await ViewModel.ChangeVote(VN, voteValue);
				if (success) VN.OnPropertyChanged(null);
			}
		}

		private async void ShowRelatedTitles(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowRelatedTitles(VN);
			MainWindow.SelectTab(typeof(VNTabViewModel));
		}

		private async void ShowTitlesByProducer(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowForProducer(VN.Producer);
			MainWindow.SelectTab(typeof(VNTabViewModel));
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
			var characterEntries = cvns.SelectMany(GetEntriesFromCharacter).Distinct(Entry.ValueComparer).ToArray();
			MainWindow.CreateAddEntriesTab(characterEntries);
		}

		private static List<Entry> GetEntriesFromCharacter(CharacterVN cvn)
		{
			var entries = new List<Entry>();
			var character = StaticHelpers.LocalDatabase.Characters[cvn.CharacterId];
			if (string.IsNullOrWhiteSpace(character.Name) || string.IsNullOrWhiteSpace(character.Original)) return entries;
			var outputParts = character.Name.Split(' ');
			var inputParts = character.Original.Split(' ');
			if (outputParts.Length != inputParts.Length)
			{
				var entry = new Entry
				{
					RoleString = "m",
					Input = character.Original,
					Output = character.Name,
					GameId = cvn.VNId,
					SeriesSpecific = true,
					Type = EntryType.Name
				};
				entries.Add(entry);
			}
			else
			{
				for (int i = 0; i < outputParts.Length; i++)
				{
					var entry = new Entry
					{
						RoleString = "m",
						Input = inputParts[i],
						Output = outputParts[i],
						GameId = cvn.VNId,
						SeriesSpecific = true,
						Type = EntryType.Name
					};
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
				var translation = MainViewModel.Translator.Translate(MainViewModel.User, null, VN.KanjiTitle, false, false);
				message = translation.Output;
			}
			catch (Exception ex)
			{
				message = ex.Message;
			}
			MainViewModel.NotificationEvent(this, message, $"Translated Title for {VN.Title}");
		}
	}
}
