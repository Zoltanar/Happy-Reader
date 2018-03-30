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
		public bool DeletePrimed {get; set;}
		private Button _deleteButton;

		public DisplayEntry(Entry entry)
		{
			Entry = entry;
			Id = entry.Id;
			User = entry.User;
			Type = entry.Type;
			Game = entry.Game;
			Role = entry.RoleString;
			Input = entry.Input;
			Output = entry.Output;
			SeriesSpecific = entry.SeriesSpecific;
			Regex = entry.Regex;
		}
		
		public bool Regex { get; set; }

		public bool SeriesSpecific { get; set; }

		public string Output { get; set; }

		public string Input { get; set; }

		public string Role { get; set; }

		public ListedVN Game { get; set; }

		public EntryType Type { get; set; }

		public User User { get; set; }

		public long Id { get; set; }

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