using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class EntriesTab : UserControl
	{
		private EntriesTabViewModel ViewModel => (EntriesTabViewModel)DataContext;

		public EntriesTab()
		{
			InitializeComponent();
			TypeDropdownColumn.ItemsSource = Enum.GetValues(typeof(EntryType));
		}

		private void AddEntries_Click(object sender, RoutedEventArgs e)
		{
			StaticMethods.MainWindow.CreateAddEntriesTab(Array.Empty<Entry>());
		}

		private void ClickDeleteButton(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (DisplayEntry)button.DataContext;
			if (item.DeletePrimed) ViewModel.DeleteEntry(item);
			else item.PrimeDeletion(button);
		}

		private void EntriesTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			var vns = StaticMethods.MainWindow.ViewModel.UserGamesViewModel.UserGameItems
				.Select(i => i.UserGame.VN)
				.Distinct()
				.Where(i => i != null)
				.ToArray();
			GameDropdownColumn.ItemsSource = vns.ToArray();
		}
	}
}
