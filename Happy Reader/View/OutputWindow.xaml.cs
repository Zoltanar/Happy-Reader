using System.Text;
using Happy_Reader.Properties;
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
            ContextLabel.Content = translationItem.Context;
            StringBuilder htmlBuilder = new StringBuilder();
            foreach (var pair in translationItem.OriginalText)
            {
                if (string.IsNullOrWhiteSpace(pair.Romaji))
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
            _webBrowser.WebBrowser.DocumentText = htmlBuilder.ToString();
        }

        internal void SetLocation(int left, int bottom, int width)
        {
            Left = left;
            Top = bottom;
            Width = width;
        }

        public void SetTextDebug()
        {
            CharacterLabel.Content = "character name";
            ContextLabel.Content = "context details";
            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.Append(@"<h1><strong><span style=""color: #00ff00;""><ruby>皐月<rt>gogatsu/satsuki</rt></ruby> Satsuki</span></strong></h1>");
            _webBrowser.WebBrowser.DocumentText = htmlBuilder.ToString();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
            SessionSettings.SetGameWindowCoords(Left, Top, Width, Height);
        }
    }
}
