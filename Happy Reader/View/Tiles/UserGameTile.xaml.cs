using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View.Tiles
{
	public partial class UserGameTile
	{
		private VnMenuItem _vnMenu;
		private bool _loaded;

		public UserGame UserGame { get; }

		// ReSharper disable once PossibleNullReferenceException
		[NotNull] public MainWindowViewModel MainViewModel => (MainWindowViewModel)Window.GetWindow(this).DataContext;

		private VnMenuItem VnMenu => _vnMenu ??= new VnMenuItem(UserGame?.VN);

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

		public void ViewDetails(object sender, EventArgs e)
		{
			var mainWindow = (MainWindow)(sender is MainWindow ? sender : Window.GetWindow(this));
			Trace.Assert(mainWindow != null, nameof(mainWindow) + " != null");
			if (UserGame.HasVN) mainWindow.OpenVNPanel(UserGame.VN);
			else mainWindow.OpenUserGamePanel(UserGame,null);

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
				MainViewModel.RemoveUserGame(this);
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
				MainViewModel.RemoveUserGame(mergeTarget.UserGame);
			}
		}

		private void LaunchProcessClick(object sender, RoutedEventArgs e) => MainViewModel.HookUserGame(UserGame, null, false);

		private void LaunchWithLeJapan(object sender, RoutedEventArgs e) => MainViewModel.HookUserGame(UserGame, null, true);

		private void ResetTimePlayed(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Confirm that you wish to reset time played.", "Confirmation Required", MessageBoxButton.OKCancel);
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

		private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ViewDetails(sender,e);
		}

		private void LaunchGame(object sender, RoutedEventArgs e)
		{
			if (UserGame.IsRunning) return;
			LaunchProcessClick(sender,e);
		}
	}
}
