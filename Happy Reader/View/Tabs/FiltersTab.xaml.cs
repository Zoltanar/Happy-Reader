using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
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
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			_viewModel = (FiltersViewModel)DataContext;
		}

		private void FilterKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete) return;
			ListBox listBox = (ListBox)sender;
			var list = (IList<VnFilter>)listBox.ItemsSource;
			foreach (VnFilter item in listBox.SelectedItems.Cast<VnFilter>().ToArray()) list.Remove(item);
			var parent = listBox.FindParent<GroupBox>();
			if (parent == PermanentFilterGroupBox)
			{
				_viewModel.SavePermanentFilter();
			}
		}

		private void DeleteFilterOnKey(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete) return;
			_viewModel.DeleteCustomFilter();
		}
	}
}
