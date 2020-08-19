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
		private readonly UserGame _viewModel;
		private readonly bool _hideImage;

		public UserGameTab(UserGame game, bool hideImage)
		{
			InitializeComponent();
			DataContext = game;
			_viewModel = game;
			_hideImage = hideImage;
		}

		private void BrowseToFolderClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Process.Start("explorer", Path.GetDirectoryName(_viewModel.FilePath));
		}

		private void ChangeFileLocationClick(object sender, System.Windows.RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			string directory = Path.GetDirectoryName(_viewModel.FilePath);
			while (!Directory.Exists(directory))
			{
				directory = directory == null ? Environment.CurrentDirectory : Directory.GetParent(directory).FullName;
			}
			dialog.InitialDirectory = directory;
			Debug.WriteLine(dialog.InitialDirectory);
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
			_viewModel.SaveVNID(VnidNameBox.Text.Length == 0 ? null : (int?)int.Parse(VnidNameBox.Text));
		}

		private void SaveHookCode(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			_viewModel.SaveHookCode(HookCodeBox.Text, null);
		}

		private static readonly Regex DigitRegex = new Regex(@"\d");

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
			if (!_hideImage) return;
			Image.Visibility = Visibility.Collapsed;
			ImageBorder.Visibility = Visibility.Collapsed;
		}
	}
}
