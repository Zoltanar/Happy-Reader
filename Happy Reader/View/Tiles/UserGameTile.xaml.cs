using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tiles
{
	public partial class UserGameTile
	{
		private VnMenuItem _vnMenu;
		private bool _loaded;

		public UserGame UserGame { get; }

		private VnMenuItem VnMenu => _vnMenu ??= new VnMenuItem() { DataContext = UserGame?.VN, VnItem = UserGame?.VN, };

		public UserGameTile()
		{
			InitializeComponent();
		}

		public UserGameTile(UserGame userGame)
		{
			InitializeComponent();
			DataContext = userGame;
			UserGame = userGame;
		}

		public void GameDetails(object sender, EventArgs e)
		{
			var mainWindow = (MainWindow)(sender is MainWindow ? sender : Window.GetWindow(this));
			Trace.Assert(mainWindow != null, nameof(mainWindow) + " != null");
			mainWindow.OpenUserGamePanel(UserGame);
		}

		public void BrowseToLocation(object sender, RoutedEventArgs e)
		{
			var directory = Directory.GetParent(UserGame.FilePath);
			while (!directory.Exists)
			{
				if (directory.Parent == null) break;
				directory = directory.Parent;
			}
			Process.Start("explorer", $"\"{directory.FullName}\"");
		}

		public void RemoveUserGame(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show($"Are you sure you want to remove {UserGame.DisplayName}?", "Confirm", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes)
			{
				// ReSharper disable once PossibleNullReferenceException
				var mainViewModel = (MainWindowViewModel)((MainWindow)Window.GetWindow(this)).DataContext;
				mainViewModel.RemoveUserGame(this);
			}
		}

		private void MergeGamesToThis(object sender, RoutedEventArgs e)
		{
			var mergeWindow = new MergeWindow(UserGame);
			var result = mergeWindow.ShowDialog();
			Debug.Assert(result != null, nameof(result) + " != null");
			if (!result.Value) return;
			var additionalTimePlayedTicks = mergeWindow.MergeResults.Sum(t => t.UserGame.TimeOpen.Ticks);
			var additionalTimePlayed = new TimeSpan(additionalTimePlayedTicks);
			UserGame.MergeTimePlayed(additionalTimePlayed);
			foreach (var mergeTarget in mergeWindow.MergeResults)
			{
				// ReSharper disable once PossibleNullReferenceException
				var mainViewModel = (MainWindowViewModel)((MainWindow)Window.GetWindow(this)).DataContext;
				mainViewModel.RemoveUserGame(mergeTarget.UserGame);
			}
		}

		private void OpenVNDetails(object sender, RoutedEventArgs e)
		{
			if (!UserGame.VNID.HasValue) throw new InvalidOperationException("UserGame does not have attached VN.");
			var mainWindow = (MainWindow)(sender is MainWindow ? sender : Window.GetWindow(this));
			Trace.Assert(mainWindow != null, nameof(mainWindow) + " != null");
			mainWindow.OpenVNPanel(UserGame.VN);
		}

		private void LaunchWithLeJapan(object sender, RoutedEventArgs e)
		{
			var mainWindow = (MainWindow)(sender is MainWindow ? sender : Window.GetWindow(this));
			Trace.Assert(mainWindow != null, nameof(mainWindow) + " != null");
			var viewModel = (MainWindowViewModel)mainWindow.DataContext;
			viewModel.HookUserGame(UserGame, null,true);
		}

		private void ResetTimePlayed(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Confirm that you wish to reset time played.", "Confirmation Required",
				MessageBoxButton.OKCancel);
			if (result != MessageBoxResult.OK) return;
			UserGame.ResetTimePlayed();
		}

		private void OpenVnSubmenu(object sender, RoutedEventArgs e)
		{
			VnMenu.ContextMenuOpened();
		}

		private void UserGameTile_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			VnMenu.TransferItems(VnMenuParent);
			_loaded = true;
		}
	}
}
