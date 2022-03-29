using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View.Tabs
{
	public partial class AddEntriesTab : UserControl
	{
		private readonly MainWindowViewModel _mainViewModel;

		public AddEntriesTab(MainWindowViewModel mainViewModel, IEnumerable<Entry> entries)
		{
			_mainViewModel = mainViewModel;
			InitializeComponent();
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
			TranslationEngine.Translator.Instance.RefreshEntries = true;
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
					case EntryType.Translation:
						entry.RoleString = "n";
						break;
                    case EntryType.Name:
					case EntryType.Proxy:
					case EntryType.ProxyMod:
						ResponseLabel.Content = $@"Entries of type '{entry.Type}' require a role.";
						return false;
				}
			}
			//whitespace is valid
			if (!string.IsNullOrEmpty(entry.Input)) return true;
			ResponseLabel.Content = @"Input field must not be empty.";
			return false;
		}

		private void EntriesGrid_OnLoadingRow(object sender, DataGridRowEventArgs e)
		{
			if (e.Row?.DataContext is not DisplayEntry item || item.Entry == null) return;
			if (string.IsNullOrWhiteSpace(item.Entry.Input))
			{
				item.Type = EntryType.Name;
                item.Role = Entry.DefaultNameRole;
            }
			item.Entry.UserId = StaticMethods.MainWindow.ViewModel.User.Id;
			var game = StaticMethods.MainWindow.ViewModel.UserGame;
			if (game?.VNID.HasValue ?? false) item.Entry.SetGameId(game.VNID, false);
			else if (game != null) item.Entry.SetGameId((int)game.Id, true);
		}

		[UsedImplicitly]
		private bool EntryGameFilter(string input, object item)
		{
			//Short input is not filtered to prevent excessive loading times
			if (input.Length <= 4) return false;
			var gameData = (EntryGame)item;
			var result = gameData.ToString().ToLowerInvariant().Contains(input.ToLowerInvariant());
			return result;
		}

		private void SetEntryGameEnter(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			UpdateEntryGame(sender);
		}

		private void SetEntryGameLeftClick(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Released) return;
			UpdateEntryGame(sender);
		}

		private void UpdateEntryGame(object sender)
		{
			var acb = (AutoCompleteBox)sender;
			var binding = acb.GetBindingExpression(AutoCompleteBox.TextProperty);
			Debug.Assert(binding != null, nameof(binding) + " != null");
			binding.UpdateSource();
		}
	}
}
