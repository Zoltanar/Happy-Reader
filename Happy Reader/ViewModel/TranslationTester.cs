using System.ComponentModel;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
    public class TranslationTester : INotifyPropertyChanged
    {
        public string OriginalText { get; set; }
        public string Stage1 { get; set; }
        public string Stage2 { get; set; }
        public string Stage3 { get; set; }
        public string Stage4 { get; set; }
        public string Stage5 { get; set; }
        public string Stage6 { get; set; }
        public string Stage7 { get; set; }
        public ListedVN Game { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        public void Test(User user)
        {
            if (string.IsNullOrWhiteSpace(OriginalText)) return;
            var translation = Translator.Translate(user, Game, OriginalText);
            Stage1 = translation.Results[1];
            Stage2 = translation.Results[2];
            Stage3 = translation.Results[3];
            Stage4 = translation.Results[4];
            Stage5 = translation.Results[5];
            Stage6 = translation.Results[6];
            Stage7 = translation.Results[7];
            OnPropertyChanged(null);
        }
    }
}