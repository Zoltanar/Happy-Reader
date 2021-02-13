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

		private readonly UserGame _viewModel;
		private readonly bool _isForVN;

		public UserGameTab(UserGame game, bool isForVN)
		{
			InitializeComponent();
			DataContext = game;
			_viewModel = game;
			_isForVN = isForVN;
		}

		private void BrowseToFolderClick(object sender, RoutedEventArgs e)
		{
			Process.Start("explorer", Path.GetDirectoryName(_viewModel.FilePath));
		}

		private void ChangeFileLocationClick(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			var directory = new DirectoryInfo(Path.GetDirectoryName(_viewModel.FilePath) ?? Environment.CurrentDirectory);
			Debug.Assert(directory != null, nameof(directory) + " != null");
			while (!directory.Exists)
			{
				directory = directory.Parent ?? new DirectoryInfo(Environment.CurrentDirectory);
			}
			dialog.InitialDirectory = directory.FullName;
			var result = dialog.ShowDialog();
			if (result ?? false) _viewModel.ChangeFilePath(dialog.FileName);
		}

		private void SaveUserDefinedName(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			_viewModel.SaveUserDefinedName(DisplayNameBox.Text);
		}
		private void SaveTag(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			_viewModel.SaveTag(TagBox.Text);
		}

		private void SaveVNID(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			var priorVN = _viewModel.VN;
			var result = _viewModel.SaveVNID(VnidNameBox.Text.Length == 0 ? null : (int?)int.Parse(VnidNameBox.Text));
			if (result) StaticMethods.MainWindow.OpenVNPanel(_viewModel.VN);
			else StaticMethods.MainWindow.OpenUserGamePanel(_viewModel, priorVN);
		}

		private void SaveHookCode(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			_viewModel.SaveHookCode(HookCodeBox.Text);
		}

		private void DigitsOnly(object sender, TextCompositionEventArgs e)
		{
			if (!DigitRegex.IsMatch(e.Text)) e.Handled = true;
		}

		private void SaveLaunchPath(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			_viewModel.SaveLaunchPath(LaunchPathBox.Text);
		}

		private void UserGameTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			Image.Visibility = _isForVN ? Visibility.Collapsed : Visibility.Visible;
			ImageBorder.Visibility = _isForVN ? Visibility.Collapsed : Visibility.Visible;
			ImageColumn.Width = _isForVN ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
			DataColumn.Width = _isForVN ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
		}
	}
}
