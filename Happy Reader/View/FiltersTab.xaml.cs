using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	/// <summary>
	/// Interaction logic for FiltersTab.xaml
	/// </summary>
	public partial class FiltersTab : UserControl
	{
		// ReSharper disable once NotAccessedField.Local
		private FiltersViewModel _viewModel;

		public FiltersTab()
		{
			InitializeComponent();
		}

		private void FiltersTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			_viewModel = (FiltersViewModel)DataContext;
		}

		private void FilterKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete) return;
			ListBox listBox = (ListBox)sender;
			var list = (IList<VnFilter>)listBox.ItemsSource;
			foreach (VnFilter item in listBox.SelectedItems.Cast<VnFilter>().ToArray()) list.Remove(item);
		}
	}
}
