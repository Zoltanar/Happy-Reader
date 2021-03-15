using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.Database;

namespace Happy_Reader.View.Tiles
{
	public partial class UserGameTile
	{
		private VnMenuItem _vnMenu;
		private bool _loaded;

		public UserGame UserGame { get; }
		
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
			if (UserGame.HasVN) StaticMethods.MainWindow.OpenVNPanel(UserGame.VN);
			else StaticMethods.MainWindow.OpenUserGamePanel(UserGame,null);
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
				StaticMethods.MainWindow.ViewModel.UserGamesViewModel.RemoveUserGame(this);
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
				StaticMethods.MainWindow.ViewModel.UserGamesViewModel.RemoveUserGame(mergeTarget.UserGame);
			}
		}

		private void LaunchProcessClick(object sender, RoutedEventArgs e) => StaticMethods.MainWindow.ViewModel.HookUserGame(UserGame, null, false);

		private void LaunchWithLeJapan(object sender, RoutedEventArgs e) => StaticMethods.MainWindow.ViewModel.HookUserGame(UserGame, null, true);

		private void ResetTimePlayed(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Confirm that you wish to reset time played.", "Confirmation Required", MessageBoxButton.OKCancel);
			if (result != MessageBoxResult.OK) return;
			UserGame.ResetTimePlayed();
		}

		private void OpenVnSubmenu(object sender, RoutedEventArgs e)
		{
			VnMenu.DataContext ??= UserGame.VN;
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
			if (UserGame.RunningStatus == UserGame.ProcessStatus.Off) LaunchProcessClick(sender,e);
		}

		private void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Middle) return;
			var element = (FrameworkElement) sender;
			var hitTestResult = VisualTreeHelper.HitTest(element, e.GetPosition(element));
			var userGameTile = hitTestResult.VisualHit.FindParent<UserGameTile>();
			var item = userGameTile?.DataContext;
			var userGame = item switch
			{
				UserGame iUserGame => iUserGame,
				_ => null
			};
			if (userGame == null) return;
			if (UserGame.HasVN) StaticMethods.MainWindow.OpenVNPanel(UserGame.VN, false);
			else StaticMethods.MainWindow.OpenUserGamePanel(UserGame, null, false);
		}
	}
}
