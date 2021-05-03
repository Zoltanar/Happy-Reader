using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class AddEntriesTab : UserControl
	{
		private readonly MainWindowViewModel _mainViewModel;

		public AddEntriesTab(MainWindowViewModel mainViewModel, IEnumerable<ListedVN> eligibleGames, IEnumerable<Entry> entries)
		{
			_mainViewModel = mainViewModel;
			InitializeComponent();
			GameDropdownColumn.ItemsSource = eligibleGames.ToArray();
			TypeDropdownColumn.ItemsSource = Enum.GetValues(typeof(EntryType));
			EntriesGrid.ItemsSource = entries == null ? new ObservableCollection<DisplayEntry>() : new ObservableCollection<DisplayEntry>(entries.Select(e => new DisplayEntry(e)));
		}

		private void CancelClick(object sender, System.Windows.RoutedEventArgs e)
		{
			var tabItem = (TabItem)Parent;
			var tabControl = (TabControl)tabItem.Parent;
			//tabControl.SelectedIndex = 1;
			tabControl.Items.Remove(tabItem);
		}
		private void AddEntriesClick(object sender, System.Windows.RoutedEventArgs e)
		{
			var entries = EntriesGrid.ItemsSource.Cast<DisplayEntry>().Select(de => de.Entry).ToArray();
			if (entries.Length == 0)
			{
				ResponseLabel.Content = "There are no entries to add.";
				return;
			}
			foreach (var entry in entries)
			{
				if (!ValidateEntry(entry)) return;
				entry.Time = DateTime.UtcNow;
				entry.UserId = _mainViewModel.User?.Id ?? 0;
			}

			StaticMethods.Data.AddEntries(entries);
			ResponseLabel.Content = $@"{entries.Length} entries were added.";
			_mainViewModel.EntriesViewModel.SetEntries();
			_mainViewModel.Translator.RefreshEntries = true;
			CancelClick(this, null);
		}

		private bool ValidateEntry(Entry entry)
		{
			entry.Output ??= string.Empty;
			if (entry.Regex)
			{
				try
				{
					// ReSharper disable once ReturnValueOfPureMethodIsNotUsed We just check if regex is valid
					System.Text.RegularExpressions.Regex.IsMatch(string.Empty, entry.Input);
				}
				catch (ArgumentException ex)
				{
					ResponseLabel.Content = $@"Regex '{entry.Input}' is not valid: {ex.Message}";
					return false;
				}
			}
			if (string.IsNullOrWhiteSpace(entry.RoleString))
			{
				switch (entry.Type)
				{
					case EntryType.Name:
						entry.RoleString = "m";
						break;
					case EntryType.Translation:
						entry.RoleString = "n";
						break;
					case EntryType.Proxy:
					case EntryType.ProxyMod:
						ResponseLabel.Content = $@"Entries of type '{entry.Type}' require a role.";
						return false;
				}
			}
			if (!string.IsNullOrWhiteSpace(entry.Input)) return true;
			ResponseLabel.Content = @"Input field must not be empty.";
			return false;
		}

		private void EntriesGrid_OnLoadingRow(object sender, DataGridRowEventArgs e)
		{
			var item = e.Row.DataContext as DisplayEntry;
			if (item == null || item.Entry == null) return;
			if(item.Entry.UserId == 0) item.Entry.UserId = StaticMethods.MainWindow.ViewModel.User.Id;
			item.Entry.GameId ??= StaticMethods.MainWindow.ViewModel.UserGame?.VNID;
			item.Type = EntryType.Name;
		}
	}
}
