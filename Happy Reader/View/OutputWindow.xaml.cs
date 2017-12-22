using System.Linq;
using System.Text;
using System.Windows.Controls;
using Happy_Reader.WinForms;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow
    {

        private readonly WebBrowserOverlayWf _webBrowser;
        private readonly RecentItemList<StringBuilder> _recentItems = new RecentItemList<StringBuilder>(10);
        private bool _lockedLocation;

        public OutputWindow()
        {
            InitializeComponent();
            _webBrowser = new WebBrowserOverlayWf(WebBrowserPanel);
        }

        public void SetText(TranslationItem translationItem)
        {
            CharacterLabel.Content = translationItem.Character;
            ContextLabel.Content = translationItem.RightLabel;
            StringBuilder htmlBuilder = new StringBuilder();
            var original = string.Join(string.Empty, translationItem.OriginalText.Select(x => x.Original));
            var romaji = string.Join(" ", translationItem.OriginalText.Select(x => x.Romaji));
            htmlBuilder.Append(original);
            htmlBuilder.Append("</br>");
            if (!romaji.Equals(original))
            {
                htmlBuilder.Append(romaji);
                htmlBuilder.Append("</br>");
            }
            if (!translationItem.TranslatedText.Equals(original)) htmlBuilder.Append(translationItem.TranslatedText);
            _recentItems.Add(htmlBuilder);
            string html = @"<style>
div {
    font-size:22px;
}
</style>
<body bgcolor=""#E6E6FA""><div>" + string.Join("</br></br>", _recentItems.Items);
            html += "</div></body>";
            _webBrowser.WebBrowser.DocumentText = html;
        }

        public void SetTextRuby(TranslationItem translationItem)
        {
            CharacterLabel.Content = translationItem.Character;
            ContextLabel.Content = translationItem.RightLabel;
            StringBuilder htmlBuilder = new StringBuilder();
            foreach (var (original, romaji) in translationItem.OriginalText)
            {
                if (string.IsNullOrWhiteSpace(romaji) || original == romaji)
                { htmlBuilder.Append(original); }
                else
                {
                    htmlBuilder.Append("<ruby><rb>");
                    htmlBuilder.Append(original);
                    htmlBuilder.Append("</rb><rt>");
                    htmlBuilder.Append(romaji);
                    htmlBuilder.Append("</rt></ruby>");
                }
            }
            htmlBuilder.Append("</br>");
            htmlBuilder.Append(translationItem.TranslatedText);
            _recentItems.Add(htmlBuilder);
            string html = "<div style=\"font-size:22px;\">";
            foreach (var item in _recentItems.Items)
            {
                html += item.ToString();
                html += "<br>";
            }
            html += "</div>";
            _webBrowser.WebBrowser.DocumentText = html;
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

        private void ToggleLock(object sender, System.Windows.RoutedEventArgs e)
        {
            _lockedLocation = ((CheckBox)sender).IsChecked ?? false;
        }
    }
}
