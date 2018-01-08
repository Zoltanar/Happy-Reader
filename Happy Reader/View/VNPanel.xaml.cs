using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for VNPanel.xaml
    /// </summary>
    public partial class VNPanel : UserControl
    {
        public VNPanel(ListedVN vn)
        {
            InitializeComponent();
            DataContext = vn;
        }

        private async void VNPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            await ((ListedVN)DataContext).GetRelationsAnimeScreens();
            RelationsCombobox.SelectedIndex = 0;
            AnimeCombobox.SelectedIndex = 0;
            //ScreensCombobox.SelectedIndex = 0;
        }

        private void ScrollViewer_OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scrollviewer = (ScrollViewer)sender;
            scrollviewer.CanContentScroll = true;
            if (e.Delta > 0) scrollviewer.LineLeft();
            else scrollviewer.LineRight();
            e.Handled = true;
        }
    }
}
