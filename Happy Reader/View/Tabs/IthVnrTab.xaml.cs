using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.ViewModel;
using IthVnrSharpLib;

namespace Happy_Reader.View.Tabs
{
	public partial class IthVnrTab
	{
		[DllImport("msvcr110.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int _fpreset();

		private IthViewModel _viewModel;
		private Timer _selectionTimer;

		public IthVnrTab() => InitializeComponent();

		private void OpenProcessExplorer(object sender, RoutedEventArgs e) => new ProcessExplorer(_viewModel).ShowDialog();

		private void EnterCommand(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			var textBox = (TextBox)sender;
			if (string.IsNullOrWhiteSpace(textBox.Text)) return;
			var processInfo = ProcessComboBox.SelectedItem as ProcessInfo;
			_viewModel.Commands.ProcessCommand(textBox.Text, processInfo?.Process.Id ?? 0);
		}

		private void TextThreadChanged(object sender, SelectionChangedEventArgs e)
		{
			_fpreset();
			_viewModel.OnPropertyChanged(nameof(_viewModel.SelectedTextThread));
		}

		private void IthVnrTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			_viewModel = (IthViewModel)DataContext;
		}

		private void EncodingChanged(object sender, SelectionChangedEventArgs e)
		{
			if(_viewModel?.SelectedTextThread != null) _viewModel.OnPropertyChanged(nameof(_viewModel.SelectedTextThread));
		}

		private void FinalizeButton(object sender, RoutedEventArgs e)
		{
			_viewModel.Finalize(sender, null);
			MainTextBox.Background = Brushes.DarkRed;
		}

		private void InitializeButton(object sender, RoutedEventArgs e)
		{
			_viewModel.ReInitialize();
			MainTextBox.Background = Brushes.White;
		}

		private void MainTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
		{
			if (MainTextBox.SelectedText.Length == 0) return;
			if (_selectionTimer == null)
			{
				_selectionTimer = new Timer
				{
					AutoReset = false,
					Enabled = true,
					Interval = 300
				};
				_selectionTimer.Elapsed += OutputSelectedText;
			}

			_selectionTimer.Stop();
			_selectionTimer.Start();
		}

		private void OutputSelectedText(object sender, ElapsedEventArgs e)
		{
			_viewModel.OutputSelectedText(MainTextBox.SelectedText);
			_selectionTimer?.Close();
			_selectionTimer = null;
		}
		
		private void MainTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			MainTextBox.ScrollToEnd();
		}
	}
}