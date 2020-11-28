using System;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class EntriesTab : UserControl
	{
		private EntriesTabViewModel ViewModel => (EntriesTabViewModel)DataContext;

		public EntriesTab() => InitializeComponent();

		private void AddEntries_Click(object sender, RoutedEventArgs e) 
			=> ((MainWindow)Application.Current.MainWindow).CreateAddEntriesTab(Array.Empty<Entry>());

		private void ClickDeleteButton(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (DisplayEntry)button.DataContext;
			if (item.DeletePrimed) ViewModel.DeleteEntry(item);
			else item.PrimeDeletion(button);
		}
	}
}
