using System.Text;
using Happy_Reader.WinForms;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow
    {

        private readonly WebBrowserOverlayWf _webBrowser;

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
            foreach (var pair in translationItem.OriginalText)
            {
                if (string.IsNullOrWhiteSpace(pair.Romaji) || pair.Original == pair.Romaji)
                {
                    htmlBuilder.Append(pair.Original);
                }
                else
                {
                    htmlBuilder.Append("<ruby><rb>");
                    htmlBuilder.Append(pair.Original);
                    htmlBuilder.Append("</rb><rt>");
                    htmlBuilder.Append(pair.Romaji);
                    htmlBuilder.Append("</rt></ruby>");
                }
            }
            htmlBuilder.Append("</br>");
            htmlBuilder.Append(translationItem.TranslatedText);
            _webBrowser.WebBrowser.DocumentText = "<div style=\"font-size:22px;\"> " + htmlBuilder + "</div>";
        }

        internal void SetLocation(int left, int bottom, int width)
        {
            Left = left;
            Top = bottom;
            Width = width;
        }
        
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
            //SetGameWindowCoords(Left, Top, Width, Height);
        }
    }
}
