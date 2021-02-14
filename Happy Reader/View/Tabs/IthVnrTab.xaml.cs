using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.ViewModel;
using IthVnrSharpLib;

namespace Happy_Reader.View.Tabs
{
	public partial class IthVnrTab
	{
		private IthViewModel _viewModel;

		public IthVnrTab()
		{
			InitializeComponent();
		}

		private void OpenProcessExplorer(object sender, RoutedEventArgs e) => new ProcessExplorer(_viewModel).ShowDialog();

		private void EnterCommand(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			var textBox = (TextBox)sender;
			if (string.IsNullOrWhiteSpace(textBox.Text)) return;
			var processInfo = ProcessComboBox.SelectedItem as ProcessInfo;
			_viewModel.Commands.ProcessCommand(textBox.Text, processInfo?.Id ?? 0);
		}
		
		private void IthVnrTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			_viewModel = (IthViewModel)DataContext;
			_viewModel.Selector = ThreadSelector;
		}
		
		private void FinalizeButton(object sender, RoutedEventArgs e)
		{
			_viewModel.Finalize(sender, null);
			Background = Brushes.DarkRed;
		}

		private void InitializeButton(object sender, RoutedEventArgs e)
		{
			_viewModel.ReInitialize(out var errorMessage);
			if(!string.IsNullOrWhiteSpace(errorMessage)) _viewModel.DisplayThreads.Add(new TextBlock(new Run(errorMessage)));
			Background = Brushes.White;
		}
		
		private void ShowOutputWindow(object sender, RoutedEventArgs e)
		{
			StaticMethods.MainWindow.ViewModel.OutputWindow.Show();
		}

		private void ResetOutputWindow(object sender, RoutedEventArgs e)
		{
			StaticMethods.MainWindow.ViewModel.OutputWindow.SetLocation(StaticMethods.OutputWindowStartPosition);
		}
	}
}