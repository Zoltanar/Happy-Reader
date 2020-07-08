using System;
using System.Windows.Controls;
using Happy_Apps_Core;

namespace Happy_Reader.View.Tabs
{
	/// <summary>
	/// Interaction logic for SettingsTab.xaml
	/// </summary>
	public partial class SettingsTab : UserControl
	{
		private GuiSettings _viewModel => DataContext as GuiSettings ??
		                                  throw new ArgumentNullException(
			                                  $"Expected view model to be of type {nameof(GuiSettings)}");

		public SettingsTab()
		{
			InitializeComponent();
		}

		private void SetClipboardSize(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			_viewModel.MaxClipboardSize = (int)((Slider)e.Source).Value;
		}
	}
}
