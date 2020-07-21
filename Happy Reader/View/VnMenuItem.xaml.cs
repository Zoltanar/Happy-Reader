using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View
{
	public partial class VnMenuItem : ItemsControl
	{
		[NotNull]
		private MainWindow MainWindow(FrameworkElement sender) => (MainWindow)Window.GetWindow(sender) ?? throw new ArgumentNullException(nameof(MainWindow));

		private MainWindowViewModel MainViewModel(FrameworkElement sender) => MainWindow(sender).ViewModel;
		private VNTabViewModel ViewModel(FrameworkElement sender) => MainViewModel(sender).DatabaseViewModel;

		private ListedVN VN => (ListedVN)DataContext;

		public VnMenuItem(ListedVN vn)
		{
			InitializeComponent();
			DataContext = vn;
		}

		private void BrowseToVndbPage(object sender, RoutedEventArgs e) => Process.Start($"http://vndb.org/v{VN.VNID}/");

		private void BrowseToReleasePage(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(VN.ReleaseLink)) return; //todo disable when this is true
			Process.Start(VN.ReleaseLink);
		}

		private void BrowseToExtraPage(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(StaticMethods.GSettings.ExtraPageLink)) return; //todo disable when this is true
			var title = string.IsNullOrWhiteSpace(VN.KanjiTitle) ? VN.Title : VN.KanjiTitle;
			//remove minus so search includes term
			var titleFixed = title.Replace("-", "").Replace("ー", "");
			Process.Start(StaticMethods.GSettings.ExtraPageLink.Replace("%s", titleFixed));
		}

		public void ContextMenuOpened()
		{
			if (string.IsNullOrWhiteSpace(VN.ReleaseLink))
			{
				ReleaseLinkItem.IsEnabled = false;
				ReleaseLinkItem.ToolTip = @"No link found.";
			}
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
			if (!VN.Producer?.IsFavorited ?? true) return;
			AddProducerToFavoritesItem.IsEnabled = false;
			AddProducerToFavoritesItem.ToolTip = @"Already in list.";
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
			var success = await ViewModel(menuItem).ChangeVNStatus(VN, labelsToSet);
			if (success) VN.OnPropertyChanged(null);
		}

		private async void ChangeVote(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var remove = menuItem.IsChecked;
			var header = menuItem.Header.ToString();
			var voteValue = remove ? null : header == "None" ? (int?)null : int.Parse(header);
			var success = await ViewModel(menuItem).ChangeVote(VN, voteValue);
			if (success) VN.OnPropertyChanged(null);
		}

		private async void UpdateVN(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var success = await ViewModel(menuItem).UpdateVN(VN);
			if (success) VN.OnPropertyChanged(null);
		}

		private async void ShowTitlesByProducer(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			await ViewModel(menuItem).ShowForProducer(VN.Producer);
		}

		private void CopyTitle(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(VN.Title);
		}

		private void CopyOriginalTitle(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(VN.KanjiTitle)) return; //todo disable when this is true
			Clipboard.SetText(VN.KanjiTitle);
		}

		public void TransferItems(ItemsControl parent)
		{
			foreach (var item in Items.Cast<FrameworkElement>().ToList())
			{
				Items.Remove(item);
				parent.Items.Add(item);
			}
		}
	}
}
