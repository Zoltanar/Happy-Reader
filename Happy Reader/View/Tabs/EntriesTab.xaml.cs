using System.Windows;
using System.Windows.Controls;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class EntriesTab : UserControl
	{
		private EntriesTabViewModel ViewModel { get; }

		public EntriesTab()
		{
			InitializeComponent();
			ViewModel = (EntriesTabViewModel)DataContext;
		}

		private void AddEntry_Click(object sender, RoutedEventArgs e)
			=> ((MainWindow)Application.Current.MainWindow).CreateAddEntryTab(null, null, false);

		private void ClickDeleteButton(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (DisplayEntry)button.DataContext;
			if (item.DeletePrimed)
			{
				ViewModel.DeleteEntry(item);
			}
			else
			{
				item.PrimeDeletion(button);
			}
		}
	}
}
