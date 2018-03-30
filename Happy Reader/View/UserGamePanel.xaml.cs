using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.Database;
using Microsoft.Win32;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for UserGamePanel.xaml
    /// </summary>
    public partial class UserGamePanel : UserControl
    {
        private readonly UserGame _viewModel;

        public UserGamePanel(UserGame game)
        {
            InitializeComponent();
            DataContext = game;
            _viewModel = game;
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

        private void SaveUsedDefinedName(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            _viewModel.SaveUserDefinedName(DisplayNameBox.Text);
        }

        private void SaveVNID(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            _viewModel.SaveVNID(VnidNameBox.Text.Length == 0 ? null : (int?)int.Parse(VnidNameBox.Text));
        }

        private void SaveHookCode(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            _viewModel.SaveHookCode(HookCodeBox.Text);
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
	}
}
