using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Tabs;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tiles
{
	/// <summary>
	/// Interaction logic for VNTile.xaml
	/// </summary>
	public partial class VNTile : UserControl
	{
		private ListedVN _vn;
		private VNTabViewModel ViewModel => this.FindParent<DatabaseTab>().ViewModel;

		public VNTile(ListedVN vn)
		{
			InitializeComponent();
			DataContext = vn;
			_vn = vn;
		}

		public static VNTile FromListedVN(ListedVN vn)
		{
			return new VNTile(vn);
		}

		private void BrowseToVndbPage(object sender, RoutedEventArgs e)
		{
			Process.Start($"http://vndb.org/v{_vn.VNID}/");
		}

		private void BrowseToExtraPage(object sender, RoutedEventArgs e)
		{
			Process.Start($"https://exhentai.org/?f_search={_vn.KanjiTitle}");
		}

		private void ContextMenuOpened(object sender, RoutedEventArgs e)
		{
			//clearing previous
			foreach (MenuItem item in UserListMenu.Items) item.IsChecked = false;
			foreach (MenuItem item in WishListMenu.Items) item.IsChecked = false;
			foreach (MenuItem item in VoteMenu.Items) item.IsChecked = false;

			//set new
			UserListMenu.IsChecked = _vn.UserVN?.ULStatus > UserlistStatus.None;
			WishListMenu.IsChecked = _vn.UserVN?.WLStatus > WishlistStatus.None;
			VoteMenu.IsChecked = _vn.UserVN?.Vote > 0;
			if (_vn.UserVN?.ULStatus != null) ((MenuItem)UserListMenu.Items[(int)_vn.UserVN.ULStatus]).IsChecked = true;
			if (_vn.UserVN?.WLStatus != null) ((MenuItem)WishListMenu.Items[(int)_vn.UserVN.WLStatus]).IsChecked = true;
			if (_vn.UserVN?.Vote != null)
			{
				var vote = (int)Math.Round(_vn.UserVN.Vote.Value / 10d);
				((MenuItem)VoteMenu.Items[vote]).IsChecked = true;
			}
			else ((MenuItem)VoteMenu.Items[0]).IsChecked = true;
			if (!_vn.Producer?.IsFavorited ?? true) return;
			AddProducerToFavoritesItem.IsEnabled = false;
			AddProducerToFavoritesItem.ToolTip = @"Already in list.";

		}

		private async void ChangeWishListStatus(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem) sender;
			var chosenStatus = (WishlistStatus)Enum.Parse(typeof(WishlistStatus), menuItem.Header.ToString());
			var success = await ViewModel.ChangeVNStatus(_vn, chosenStatus);
			if(success) _vn.OnPropertyChanged(null);
		}

		private async void ChangeUserListStatus(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var chosenStatus = (UserlistStatus)Enum.Parse(typeof(UserlistStatus), menuItem.Header.ToString());
			var success = await ViewModel.ChangeVNStatus(_vn, chosenStatus);
			if (success) _vn.OnPropertyChanged(null);
		}

		private async void UpdateVN(object sender, RoutedEventArgs e)
		{
			var success = await ViewModel.UpdateVN(_vn);
			if (success) _vn.OnPropertyChanged(null);
		}

		private async void ShowTitlesByProducer(object sender, RoutedEventArgs e)
		{
			 await ViewModel.ShowForProducer(_vn.Producer);
		}
	}
}
