using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
    public class TranslationTester : INotifyPropertyChanged
    {
        private readonly MainWindowViewModel _mainViewModel;

        public TranslationTester(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public void Initialize()
        {
            OnPropertyChanged(nameof(UserGameNames));
        }

        public string OriginalText { get; set; }
        public string Romaji { get; set; }
        public string Stage1 { get; set; }
        public string Stage2 { get; set; }
        public string Stage3 { get; set; }
        public string Stage4 { get; set; }
        public string Stage5 { get; set; }
        public string Stage6 { get; set; }
        public string Stage7 { get; set; }
        public ListedVN Game { get; set; }
        public string[] UserGameNames => StaticMethods.Data.UserGames.Local.Select(x => x.DisplayName).ToArray();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        public void Test()
        {
            if (string.IsNullOrWhiteSpace(OriginalText)) return;
            var translation = Translator.Translate(_mainViewModel.User, Game, OriginalText);
            Romaji = translation.Romaji;
            Stage1 = translation.Results[1];
            Stage2 = translation.Results[2];
            Stage3 = translation.Results[3];
            Stage4 = translation.Results[4];
            Stage5 = translation.Results[5];
            Stage6 = translation.Results[6];
            Stage7 = translation.Results[7];
            OnPropertyChanged(null);
        }

        public bool SelectGame(string item, out string outputText)
        {
            //if text is just numbers, parse as vnid, else, look inside display names of usergames
            if (!int.TryParse(item, out int id))
            {
                id = StaticMethods.Data.UserGames.Local.FirstOrDefault(x => x.DisplayName.Contains(item))?.VNID ?? 0;
            }
            Game = StaticHelpers.LocalDatabase.VisualNovels.SingleOrDefault(x => x.VNID == id);
            outputText = Game?.Title ?? item;
            return Game != null;

        }
    }
}