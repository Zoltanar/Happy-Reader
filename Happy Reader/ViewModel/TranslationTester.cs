using System.ComponentModel;
using System.Runtime.CompilerServices;
using Happy_Reader.Database;
using JetBrains.Annotations;

namespace Happy_Reader
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


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        public void Test(User user, Game game)
        {
            var result = Translator.Translate(user, game, OriginalText, out _);
            Stage1 = result[1];
            Stage2 = result[2];
            Stage3 = result[3];
            Stage4 = result[4];
            Stage5 = result[5];
            Stage6 = result[6];
            Stage7 = result[7];
            OnPropertyChanged(null);
        }
    }
}