using System;
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
		
		public PausableUpdateList<DisplayEntry> EntriesList { get; } = new PausableUpdateList<DisplayEntry>();

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

		public EntriesTabViewModel(Func<UserGame> getUserGame)
		{
			GetUserGame = getUserGame;
		}

		public Func<UserGame> GetUserGame { get; set; }

		public void SetEntries()
		{
			var items = (OnlyGameEntries && GetUserGame()?.VN != null
				? string.IsNullOrWhiteSpace(GetUserGame()?.VN.Series)
					? StaticMethods.Data.GetGameOnlyEntries(GetUserGame()?.VN)
					: StaticMethods.Data.GetSeriesOnlyEntries(GetUserGame()?.VN)
				: StaticMethods.Data.SqliteEntries).ToArray();
			var entries = items.Select(x => new DisplayEntry(x)).ToArray();
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				EntriesList.SetRange(entries);
			});
		}

		public void DeleteEntry(DisplayEntry displayEntry)
		{
			EntriesList.Remove(displayEntry);
			StaticMethods.Data.SqliteEntries.Remove(displayEntry.Entry, true);
			Translation.Translator.RefreshEntries = true;
			OnPropertyChanged(nameof(EntriesList));
		}

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	}
}
