using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	public partial class FiltersPane : UserControl
	{

		// ReSharper disable once NotAccessedField.Local
		private FiltersViewModel ViewModel => (FiltersViewModel) DataContext;

		public FiltersPane()
		{
			InitializeComponent();
		}

		private void FilterKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete) return;
			ListBox listBox = (ListBox)sender;
			var list = (IList<IFilter<ListedVN>>)listBox.ItemsSource;
			foreach (IFilter<ListedVN> item in listBox.SelectedItems.Cast<IFilter<ListedVN>>().ToArray()) list.Remove(item);
			var parent = listBox.FindParent<GroupBox>();
			if (parent == PermanentFilterGroupBox) ViewModel.SavePermanentFilter();
		}

		private void SaveOrGroup(object sender, RoutedEventArgs e)
		{
			var parent = ((DependencyObject)sender).FindParent<GroupBox>();
			var isPermanent = parent == PermanentFilterGroupBox;
			var filter = isPermanent ? ViewModel.PermanentFilter : ViewModel.CustomFilter;
			filter.SaveOrGroup();
			if(isPermanent) ViewModel.SavePermanentFilter();
		}

		private void DeleteCustomFilter(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteCustomFilter();
		}
	}
}
