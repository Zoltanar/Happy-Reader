using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	/// <summary>
	/// Interaction logic for TitledImage.xaml
	/// </summary>
	public partial class UserGameTile
	{
		public UserGame UserGame { get; }

		public UserGameTile()
		{
			InitializeComponent();
		}

		public UserGameTile(UserGame usergame)
		{
			InitializeComponent();
			DataContext = usergame;
			UserGame = usergame;
		}

		private void GameDetails(object sender, EventArgs e)
		{
			var tabItem = new TabItem { Header = UserGame.DisplayName, Content = new UserGamePanel(UserGame) };
			// ReSharper disable once PossibleNullReferenceException
			((MainWindow)Window.GetWindow(this)).AddTabItem(tabItem);
		}

		private void BrowseToLocation(object sender, RoutedEventArgs e)
		{
			Process.Start("explorer", Directory.GetParent(UserGame.FilePath).FullName);
		}

		private void RemoveUserGame(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show($"Are you sure you want to remove {UserGame.DisplayName}?", "Confirm", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes)
			{
				// ReSharper disable once PossibleNullReferenceException
				var mainViewModel = (MainWindowViewModel)((MainWindow)Window.GetWindow(this)).DataContext;
				mainViewModel.RemoveUserGame(this);
			}
		}
	}
}
