using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
    public class TranslationTester : INotifyPropertyChanged
    {
        private readonly MainWindowViewModel _mainViewModel;

        public TranslationTester(MainWindowViewModel mainViewModel) => _mainViewModel = mainViewModel;

        public void Initialize() => OnPropertyChanged(nameof(UserGameNames));

        private string _originalText = string.Empty;
        private bool _removeRepetition;

        public string OriginalText
        {
	        get => _originalText;
	        set
	        {
		        _originalText = value;
            OnPropertyChanged();
	        } 
        }

        public bool RemoveRepetition
        {
	        get => _removeRepetition;
	        set
	        {
		        _removeRepetition = value;
		        OnPropertyChanged();
	        }
        }

        public string Romaji { get; set; }
        public string Stage1 { get; set; }
        public string Stage2 { get; set; }
        public string Stage3 { get; set; }
        public string Stage4 { get; set; }
        public string Stage5 { get; set; }
        public string Stage6 { get; set; }
        public string Stage7 { get; set; }
        public ListedVN Game { get; set; }
        public PausableUpdateList<DisplayEntry> EntriesUsed { get; } = new();
        public string[] UserGameNames => StaticMethods.Data.SqliteUserGames.Select(x => x.DisplayName).ToArray();
    
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        public void Test()
        {
            if (string.IsNullOrWhiteSpace(OriginalText)) return;
            var translation = _mainViewModel.Translator.Translate(_mainViewModel.User, Game, OriginalText,true, RemoveRepetition);
            Romaji = translation.Romaji;
            Stage1 = translation.Results[1].Equals(OriginalText) ? "(no change)" : translation.Results[1];
            Stage2 = translation.Results[2].Equals(Stage1) ? "(no change)" : translation.Results[2];
            Stage3 = translation.Results[3].Equals(Stage2) ? "(no change)" : translation.Results[3];
            Stage4 = translation.Results[4].Equals(Stage3) ? "(no change)" : translation.Results[4];
            Stage5 = translation.Results[5].Equals(Stage4) ? "(no change)" : translation.Results[5];
            Stage6 = translation.Results[6].Equals(Stage5) ? "(no change)" : translation.Results[6];
            Stage7 = translation.Results[7].Equals(Stage6) ? "(no change)" : translation.Results[7];
            SetEntries(translation.GetEntriesUsed());
            OnPropertyChanged(null);
        }

        public bool SelectGame(string item, out string outputText)
        {
            //if text is just numbers, parse as vnid, else, look inside display names of user games
            if (!int.TryParse(item, out int id))
            {
                id = StaticMethods.Data.SqliteUserGames.FirstOrDefault(x => x.DisplayName.Contains(item))?.VNID ?? 0;
            }
            Game = StaticHelpers.LocalDatabase.VisualNovels[id];
            outputText = Game?.Title ?? item;
            return Game != null;

        }

        public void SetEntries(IEnumerable<Entry> entries)
        {
	        var displayEntries = entries.Select(x => new DisplayEntry(x)).ToArray();
	        Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
	        Application.Current.Dispatcher.Invoke(() =>
	        {
		        EntriesUsed.SetRange(displayEntries);
	        });
        }

        public void DeleteEntry(DisplayEntry displayEntry)
        {
	        EntriesUsed.Remove(displayEntry);
	        StaticMethods.Data.Entries.Remove(displayEntry.Entry);
	        StaticMethods.Data.SaveChanges();
	        Translation.Translator.RefreshEntries = true;
	        OnPropertyChanged(nameof(EntriesUsed));
        }
  }
}