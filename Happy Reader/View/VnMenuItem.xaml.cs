using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View
{
	/// <summary>
	/// Interaction logic for VnMenuItem.xaml
	/// </summary>
	public partial class VnMenuItem : MenuItem
	{
		public static readonly DependencyProperty VnItemProperty = DependencyProperty.Register(
			nameof(VnItem),
			typeof(ListedVN),
			typeof(VnMenuItem),
			new FrameworkPropertyMetadata(null)
		);

		[NotNull]
		private MainWindow MainWindow => (MainWindow)Window.GetWindow(this) ?? throw new ArgumentNullException(nameof(MainWindow));

		private MainWindowViewModel MainViewModel => MainWindow.ViewModel;
		private ViewModel.VNTabViewModel ViewModel => MainViewModel.DatabaseViewModel;

		public ListedVN VnItem
		{
			get => (ListedVN) GetValue(VnItemProperty);
			set => SetValue(VnItemProperty,value);
		}

		public VnMenuItem()
		{
			InitializeComponent();
		}

		private void BrowseToVndbPage(object sender, RoutedEventArgs e)
		{
			Process.Start($"http://vndb.org/v{VnItem.VNID}/");
		}

		private void BrowseToReleasePage(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(VnItem.ReleaseLink)) return;
			Process.Start(VnItem.ReleaseLink);
		}

		private void BrowseToExtraPage(object sender, RoutedEventArgs e)
		{
			var title = string.IsNullOrWhiteSpace(VnItem.KanjiTitle) ? VnItem.Title : VnItem.KanjiTitle;
			//remove minus so search includes term
			var titleFixed = title.Replace("-", "").Replace("ー", "");
			Process.Start(StaticMethods.GSettings.ExtraPageLink.Replace("%s", titleFixed));
		}

		private void ContextMenuOpened(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(VnItem.ReleaseLink))
			{
				ReleaseLinkItem.IsEnabled = false;
				ReleaseLinkItem.ToolTip = @"No link found.";
			}
			//clearing previous
			foreach (MenuItem item in Labels.Items) item.IsChecked = false;
			foreach (MenuItem item in VoteMenu.Items) item.IsChecked = false;

			//set new
			Labels.IsChecked = VnItem.UserVN?.Labels.Any(l => l != UserVN.LabelKind.Voted) ?? false;
			VoteMenu.IsChecked = VnItem.UserVN?.Vote > 0;
			foreach (var label in VnItem.UserVN?.Labels.AsEnumerable() ?? Array.Empty<UserVN.LabelKind>())
			{
				var labelItem = Labels.Items.Cast<MenuItem>().FirstOrDefault(menuItems => ((string)menuItems.Tag).EndsWith(label.ToString()));
				if (labelItem != null) labelItem.IsChecked = true;
			}
			if (VnItem.UserVN?.Vote != null)
			{
				if (VnItem.UserVN.Vote % 10 == 0)
				{
					var vote = (int)Math.Round(VnItem.UserVN.Vote.Value / 10d);
					((MenuItem)VoteMenu.Items[vote]).IsChecked = true;
				}
				else VoteMenu.Items.Cast<MenuItem>().Last().IsChecked = true;
			}
			else ((MenuItem)VoteMenu.Items[0]).IsChecked = true;
			if (!VnItem.Producer?.IsFavorited ?? true) return;
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
			if (remove) labelsToSet = VnItem.UserVN.Labels.Except(labels).ToHashSet();
			else
			{
				labelsToSet = new HashSet<UserVN.LabelKind>();
				if (VnItem.UserVN?.Labels.Contains(UserVN.LabelKind.Voted) ?? false) labelsToSet.Add(UserVN.LabelKind.Voted);
				labelsToSet.UnionWith(labels);
			}
			var success = await ViewModel.ChangeVNStatus(VnItem, labelsToSet);
			if (success) VnItem.OnPropertyChanged(null);
		}

		private async void ChangeVote(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var remove = menuItem.IsChecked;
			var header = menuItem.Header.ToString();
			var voteValue = remove ? null : header == "None" ? (int?)null : int.Parse(header);
			var success = await ViewModel.ChangeVote(VnItem, voteValue);
			if (success) VnItem.OnPropertyChanged(null);
		}

		private async void UpdateVN(object sender, RoutedEventArgs e)
		{
			var success = await ViewModel.UpdateVN(VnItem);
			if (success) VnItem.OnPropertyChanged(null);
		}

		private async void ShowTitlesByProducer(object sender, RoutedEventArgs e)
		{
			await ViewModel.ShowForProducer(VnItem.Producer);
		}

		private void CopyTitle(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(VnItem.Title);
		}

		private void CopyOriginalTitle(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(VnItem.KanjiTitle)) return;
			Clipboard.SetText(VnItem.KanjiTitle);
		}
	}
}
