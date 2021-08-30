using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using IthVnrSharpLib;
using IthVnrSharpLib.Properties;

namespace Happy_Reader.View
{
	public partial class TextThreadPanel : UserControl
	{
		private readonly ToolTip _mouseoverTip;
		private readonly IthVnrViewModel _ithViewModel;
		private TextThread ViewModel => (TextThread)DataContext;

		[UsedImplicitly]
		public TextThreadPanel()
		{
			InitializeComponent();
			_mouseoverTip = StaticMethods.CreateMouseoverTooltip(MainTextBox, PlacementMode.Right);
		}

		public TextThreadPanel(TextThread textThread, IthVnrViewModel ithViewModel) : this()
		{
			_ithViewModel = ithViewModel;
			DataContext = textThread;
			Tag = textThread;
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

		private void SaveHookCode(object sender, RoutedEventArgs e) => _ithViewModel.SaveHookCode(ViewModel);

		private void ClearText(object sender, RoutedEventArgs e) => ViewModel.Clear(true);

		private void OnMouseover(object sender, MouseEventArgs e)
		{
			if (!StaticMethods.MainWindow.ViewModel.SettingsViewModel.TranslatorSettings.MouseoverDictionary) return;
			var mousePoint = Mouse.GetPosition(MainTextBox);
			var textPosition = MainTextBox.GetCharacterIndexFromPoint(mousePoint, false);
			if (textPosition == -1) return;
			var text = MainTextBox.Text.Substring(textPosition);
			StaticMethods.UpdateTooltip(_mouseoverTip, text);
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			if (_mouseoverTip?.IsOpen ?? false) _mouseoverTip.IsOpen = false;
		}
	}
}
