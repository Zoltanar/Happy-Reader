using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;

namespace Happy_Reader.ViewModel
{
	public class DisplayEntry
	{
		public readonly Entry Entry;
		private Timer _deleteEntryTimer;
		public bool DeletePrimed { get; set; }
		private Button _deleteButton;

		public DisplayEntry() : this(new Entry())
		{ }

		public DisplayEntry(Entry entry)
		{
			Entry = entry;
		}

		public bool Disabled
		{
			get => Entry.Disabled;
			set
			{
				if (Entry.Disabled == value) return;
				Entry.Disabled = value;
				StaticMethods.MainWindow.ViewModel.Translator.RefreshEntries = true;
				Entry.ReadyToUpsert = true;
			}
		}

		public bool Regex
		{
			get => Entry.Regex;
			set
			{
				if (Entry.Regex == value) return;
				Entry.Regex = value;
				Entry.ReadyToUpsert = true;
			}
		}

		public bool SeriesSpecific
		{
			get => Entry.SeriesSpecific;
			set
			{
				if (Entry.SeriesSpecific == value) return;
				Entry.SeriesSpecific = value;
				StaticMethods.MainWindow.ViewModel.Translator.RefreshEntries = true;
				Entry.ReadyToUpsert = true;
			}
		}

		public string Output
		{
			get => Entry.Output;
			set
			{
				if (Entry.Output == value) return;
				Entry.Output = value;
				Entry.ReadyToUpsert = true;
			}
		}

		public string Input
		{
			get => Entry.Input;
			set
			{
				if (Entry.Input == value) return;
				Entry.Input = value;
				Entry.ReadyToUpsert = true;
			}
		}

		public string Role
		{
			get => Entry.RoleString;
			set
			{
				if (Entry.RoleString == value) return;
				Entry.RoleString = value;
				Entry.ReadyToUpsert = true;
			}
		}

		public EntryGame GameData
		{
			get => Entry.GameData;
			set
			{
				if (Entry.GameData.Equals(value)) return;
				Entry.SetGameId(value.GameId,value.IsUserGame);
				Entry.ReadyToUpsert = true;
			}
		}

		public EntryType Type
		{
			get => Entry.Type;
			set
			{
				if (Entry.Type == value) return;
				Entry.Type = value;
				Entry.ReadyToUpsert = true;
			}
		}

		public User User => Entry.User;

		public long Id => Entry.Id;

		public void PrimeDeletion(Button button)
		{
			_deleteEntryTimer = new Timer(2500);
			_deleteButton = button;
			_deleteEntryTimer.Elapsed += RevertPrimeDeletion;
			_deleteButton.Content = "Press again to confirm";
			_deleteButton.Background = Brushes.Red;
			DeletePrimed = true;
			_deleteEntryTimer.Start();
		}

		private void RevertPrimeDeletion(object sender, ElapsedEventArgs e)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				_deleteButton.Content = "Delete";
				_deleteButton.Background = Brushes.CornflowerBlue;
			});
			DeletePrimed = false;
			_deleteEntryTimer?.Stop();
			_deleteEntryTimer = null;
		}
	}
}