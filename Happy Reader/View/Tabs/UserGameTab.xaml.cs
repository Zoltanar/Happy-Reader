using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.Database;
using Microsoft.Win32;

namespace Happy_Reader.View.Tabs
{
	public partial class UserGameTab : UserControl
	{
		private static readonly Regex DigitRegex = new(@"\d");

		private readonly bool _isForVN;

		public readonly UserGame ViewModel;

		public UserGameTab(UserGame game, bool isForVN)
		{
			InitializeComponent();
			DataContext = game;
			ViewModel = game;
			_isForVN = isForVN;
		}

		private void BrowseToFolderClick(object sender, RoutedEventArgs e)
		{
			Process.Start("explorer", Path.GetDirectoryName(ViewModel.FilePath));
		}

		private void ChangeFileLocationClick(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			var directory = new DirectoryInfo(Path.GetDirectoryName(ViewModel.FilePath) ?? Environment.CurrentDirectory);
			Debug.Assert(directory != null, nameof(directory) + " != null");
			while (!directory.Exists)
			{
				directory = directory.Parent ?? new DirectoryInfo(Environment.CurrentDirectory);
			}
			dialog.InitialDirectory = directory.FullName;
			var result = dialog.ShowDialog();
			if (result ?? false) ViewModel.ChangeFilePath(dialog.FileName);
		}

		private void SaveUserDefinedName(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			ViewModel.SaveUserDefinedName(DisplayNameBox.Text);
			StaticMethods.MainWindow.ViewModel.OnPropertyChanged(nameof(Happy_Reader.ViewModel.MainWindowViewModel.UserGame));
		}
		private void SaveTag(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			ViewModel.SaveTag(TagBox.Text);
		}

		private void SaveVNID(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			var priorVN = ViewModel.VN;
			var result = ViewModel.SaveVNID(VnidNameBox.Text.Length == 0 ? null : (int?)int.Parse(VnidNameBox.Text));
			if (result) StaticMethods.MainWindow.OpenVNPanel(ViewModel.VN);
			else StaticMethods.MainWindow.OpenUserGamePanel(ViewModel, priorVN);
		}

		private void SaveHookCode(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			ViewModel.SaveHookCode(HookCodeBox.Text);
		}

		private void DigitsOnly(object sender, TextCompositionEventArgs e)
		{
			if (!DigitRegex.IsMatch(e.Text)) e.Handled = true;
		}

		private void SaveLaunchPath(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			ViewModel.SaveLaunchPath(LaunchPathBox.Text);
		}

		private void UserGameTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			Image.Visibility = _isForVN ? Visibility.Collapsed : Visibility.Visible;
			ImageBorder.Visibility = _isForVN ? Visibility.Collapsed : Visibility.Visible;
			ImageColumn.Width = _isForVN ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
			DataColumn.Width = _isForVN ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
			//ViewModel.OnPropertyChanged(nameof(ViewModel.FileExists));
		}
	}
}
