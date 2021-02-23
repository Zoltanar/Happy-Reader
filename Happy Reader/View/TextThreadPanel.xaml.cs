using System.Windows;
using System.Windows.Controls;
using IthVnrSharpLib;
using IthVnrSharpLib.Properties;

namespace Happy_Reader.View
{
	public partial class TextThreadPanel : UserControl
	{
		public TextThread ViewModel => (TextThread)DataContext;
		public IthVnrViewModel IthViewModel { get; }

		[UsedImplicitly]
		public TextThreadPanel()
		{
			InitializeComponent();
		}

		public TextThreadPanel(TextThread textThread, IthVnrViewModel ithViewModel) : this()
		{
			IthViewModel = ithViewModel;
			DataContext = textThread;
		}

		private void EncodingChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ViewModel.IsDisplay) ViewModel.OnPropertyChanged(nameof(ViewModel.Text));
		}

		private void MainTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			if (ViewModel.IsDisplay) MainTextBox.ScrollToEnd();
		}
		
		private void DisplayToggled(object sender, RoutedEventArgs e)
		{
			MainTextBox.Visibility = ViewModel.IsDisplay ? Visibility.Visible : Visibility.Collapsed;
			if (ViewModel.IsDisplay) ViewModel.OnPropertyChanged(nameof(ViewModel.Text));
		}

		private void StopHideThread(object sender, RoutedEventArgs e)
		{
			ViewModel.IsDisplay = false;
			ViewModel.IsPosting = false;
			ViewModel.IsPaused = true;
			DisplayToggled(sender, e);
		}
	}
}
