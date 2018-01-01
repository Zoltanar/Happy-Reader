using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow
    {
        private readonly RecentItemList<Paragraph[]> _recentItems = new RecentItemList<Paragraph[]>(10);
        private bool _lockedLocation;
        private bool _fullScreen;

        public OutputWindow()
        {
            InitializeComponent();
        }

        public void SetText(Translation translation)
        {
            var originalP = new Paragraph(new Run(translation.Original));
            originalP.Inlines.FirstInline.Foreground = Brushes.Ivory;
            var romajiP = new Paragraph(new Run(translation.Romaji));
            romajiP.Inlines.FirstInline.Foreground = Brushes.Pink;
            var translatedP = new Paragraph(new Run(translation.Output));
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
            if(_fullScreen) Activate();
        }
        
        internal void SetLocation(int left, int bottom, int width)
        {
            if (_lockedLocation) return;
            Left = left + 0.25*width;
            Top = bottom - Height;
            Width = width*0.5;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ToggleLock(object sender, RoutedEventArgs e)
        {
            _lockedLocation = ((CheckBox)sender).IsChecked ?? false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>Close();

        private void ToggleFullScreen(object sender, RoutedEventArgs e)
        {
            _fullScreen = ((CheckBox)sender).IsChecked ?? false;
        }
    }
}
