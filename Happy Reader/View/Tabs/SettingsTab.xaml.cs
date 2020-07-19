using System;
using System.Windows.Controls;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class SettingsTab : UserControl
	{
		private SettingsViewModel ViewModel => DataContext as SettingsViewModel ?? throw new ArgumentNullException($"Expected view model to be of type {nameof(SettingsViewModel)}");

		public SettingsTab() => InitializeComponent();

		private void SetClipboardSize(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			ViewModel.TranslatorSettings.MaxClipboardSize = (int)((Slider)e.Source).Value;
		}
	}
}
