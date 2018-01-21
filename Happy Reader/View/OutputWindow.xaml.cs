using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.ViewModel;

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
        private bool _original = true;
        private bool _romaji = true;
        private readonly GridLength _settingsColumnLength;
        private readonly MainWindow _mainWindow;

        public OutputWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _settingsColumnLength = SettingsColumn.Width;
        }

        public void SetText(Translation translation)
        {
            var blocks = new System.Collections.Generic.List<Paragraph>(3);
            if (_original && !string.IsNullOrWhiteSpace(translation.Original))
            {
                var originalP = new Paragraph(new Run(translation.Original));
                originalP.Inlines.FirstInline.Foreground = Brushes.Ivory;
                blocks.Add(originalP);
            }
            if (_romaji && !string.IsNullOrWhiteSpace(translation.Romaji))
            {
                var romajiP = new Paragraph(new Run(translation.Romaji));
                romajiP.Inlines.FirstInline.Foreground = Brushes.Pink;
                blocks.Add(romajiP);
            }
            var translatedP = new Paragraph(new Run(translation.Output));
            translatedP.Inlines.FirstInline.Foreground = Brushes.GreenYellow;
            blocks.Add(translatedP);
            blocks.Add(new Paragraph());
            foreach (var block in blocks)
            {
                block.Margin = new Thickness(0);
                block.TextAlignment = TextAlignment.Center;
            }
            _recentItems.Add(blocks.ToArray());
            var doc = new FlowDocument();
            doc.Blocks.AddRange(_recentItems.Items.SelectMany(x => x));
            DebugTextbox.Document = doc;
            if (_fullScreen) Activate();
        }

        internal void SetLocation(int left, int bottom, int width)
        {
            if (_lockedLocation) return;
            Left = left + 0.25 * width;
            Top = bottom - Height;
            Width = width * 0.5;
        }

        private void DragOnMouseButton(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ShowSettings(object sender, RoutedEventArgs e)
        {
            // ReSharper disable once PossibleInvalidOperationException
            SettingsColumn.Width = ((ToggleButton)sender).IsChecked.Value ? _settingsColumnLength : new GridLength(0);
        }

        private void ToggleLock(object sender, RoutedEventArgs e)
        {
            _lockedLocation = ((CheckBox)sender).IsChecked ?? false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();

        private void ToggleFullScreen(object sender, RoutedEventArgs e) => _fullScreen = ((CheckBox)sender).IsChecked ?? false;

        private void ToggleOriginal(object sender, RoutedEventArgs e) => _original = ((CheckBox)sender).IsChecked ?? false;

        private void ToggleRomaji(object sender, RoutedEventArgs e) => _romaji = ((CheckBox)sender).IsChecked ?? false;

        private void OutputWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = (OutputWindowViewModel)DataContext;
            viewModel.Initialize(_mainWindow, DebugTextbox);
        }
    }
}
