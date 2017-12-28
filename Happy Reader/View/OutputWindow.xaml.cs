using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow
    {
        private readonly RecentItemList<Paragraph[]> _recentItems = new RecentItemList<Paragraph[]>(10);
        private bool _lockedLocation;

        public OutputWindow()
        {
            InitializeComponent();
        }

        public void SetText(TranslationItem translationItem)
        {
            CharacterLabel.Content = translationItem.Character;
            ContextLabel.Content = translationItem.RightLabel;
            var original = string.Join(string.Empty, translationItem.OriginalText.Select(x => x.Original));
            var romaji = string.Join(" ", translationItem.OriginalText.Select(x => x.Romaji));
            var originalP = new Paragraph(new Run(original));
            originalP.Inlines.FirstInline.Foreground = Brushes.Ivory;
            var romajiP = new Paragraph(new Run(romaji));
            romajiP.Inlines.FirstInline.Foreground = Brushes.Pink;
            var translatedP = new Paragraph(new Run(translationItem.TranslatedText));
            translatedP.Inlines.FirstInline.Foreground = Brushes.GreenYellow;
            var blocks = new[] {originalP,romajiP,translatedP, new Paragraph()};
            foreach (var block in blocks)
            {
                block.Margin = new Thickness(0);
                block.TextAlignment = TextAlignment.Center;
            }
            _recentItems.Add( blocks);
            var doc = new FlowDocument();
            doc.Blocks.AddRange(_recentItems.Items.SelectMany(x => x));
            DebugTextbox.Document = doc;
        }
        
        internal void SetLocation(int left, int bottom, int width)
        {
            if (_lockedLocation) return;
            Left = left;
            Top = bottom;
            Width = width;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
            //SetGameWindowCoords(Left, Top, Width, Height);
        }

        private void ToggleLock(object sender, RoutedEventArgs e)
        {
            _lockedLocation = ((CheckBox)sender).IsChecked ?? false;
        }
    }
}
