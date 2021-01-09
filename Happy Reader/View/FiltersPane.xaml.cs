using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	public partial class FiltersPane : UserControl
	{

		// ReSharper disable once NotAccessedField.Local
		private FiltersViewModelBase ViewModel => (FiltersViewModelBase)DataContext;

		public FiltersPane()
		{
			InitializeComponent();
		}

		private void FilterKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete) return;
			ListBox listBox = (ListBox)sender;
			var list = (System.Collections.IList)listBox.ItemsSource;
			var selected = listBox.SelectedItems;
			System.Array array = new object[selected.Count];
			selected.CopyTo(array,0);
			foreach (var item in array) list.Remove(item);
			var parent = listBox.FindParent<GroupBox>();
			if (parent == PermanentFilterGroupBox) ViewModel.SavePermanentFilter();
		}

		private void SaveOrGroup(object sender, RoutedEventArgs e)
		{
			var parent = ((DependencyObject)sender).FindParent<GroupBox>();
			var isPermanent = parent == PermanentFilterGroupBox;
			ViewModel.SaveOrGroup(isPermanent);
		}

		private void DeleteCustomFilter(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteCustomFilter();
		}
	}
}
