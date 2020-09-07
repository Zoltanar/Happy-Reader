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

		public DisplayEntry(Entry entry)
		{
			Entry = entry;
			Type = entry.Type;
			Role = entry.RoleString;
			Input = entry.Input;
			Output = entry.Output;
			SeriesSpecific = entry.SeriesSpecific;
			Regex = entry.Regex;
		}

		public bool Regex
		{
			get => Entry.Regex;
			set => Entry.Regex = value;
		}

		public bool SeriesSpecific
		{
			get => Entry.SeriesSpecific;
			set => Entry.SeriesSpecific = value;
		}

		public string Output
		{
			get => Entry.Output;
			set => Entry.Output = value;
		}

		public string Input
		{
			get => Entry.Input;
			set => Entry.Input = value;
		}

		public string Role
		{
			get => Entry.RoleString;
			set => Entry.RoleString = value;
		}

		public ListedVN Game => Entry.Game;

		public EntryType Type
		{
			get => Entry.Type;
			set => Entry.Type = value;
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