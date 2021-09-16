using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Happy_Reader.Database;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
	public class EntriesTabViewModel : INotifyPropertyChanged
	{
		private bool _onlyGameEntries;

		public event PropertyChangedEventHandler PropertyChanged;

		public static PausableUpdateList<EntryGame> EntryGames { get; } = new();

		public PausableUpdateList<DisplayEntry> EntriesList { get; } = new();

		public bool OnlyGameEntries
		{
			get => _onlyGameEntries;
			set
			{
				if (_onlyGameEntries == value) return;
				_onlyGameEntries = value;
				SetEntries();
			}
		}
		
		public void SetEntryGames()
		{
				var entryGames = StaticMethods.Data.UserGames
				.Select(i => i.VNID.HasValue ? new EntryGame(i.VNID, false,true) : new EntryGame((int)i.Id,true, true))
				.Distinct()
				.ToArray();
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				EntryGames.SetRange(entryGames);
				OnPropertyChanged(nameof(EntryGames));
			});
		}

		public void SetEntries()
		{
			var entryGame = StaticMethods.MainWindow.ViewModel.TestViewModel.EntryGame;
			var items = (OnlyGameEntries && entryGame.GameId.HasValue ? StaticMethods.Data.GetSeriesOnlyEntries(entryGame) : StaticMethods.Data.Entries).ToArray();
			var entries = items.Select(x => new DisplayEntry(x)).ToArray();
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				EntriesList.SetRange(entries);
				OnPropertyChanged(nameof(EntriesList));
			});
		}

		public void DeleteEntry(DisplayEntry displayEntry)
		{
			EntriesList.Remove(displayEntry);
			StaticMethods.Data.Entries.Remove(displayEntry.Entry, true);
			TranslationEngine.Translator.Instance.RefreshEntries = true;
			OnPropertyChanged(nameof(EntriesList));
		}

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	}
}
