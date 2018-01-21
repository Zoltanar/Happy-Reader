using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for VNPanel.xaml
    /// </summary>
    public partial class VNPanel : UserControl
    {
        private readonly ListedVN _viewModel;
        public VNPanel(ListedVN vn)
        {
            InitializeComponent();
            _viewModel = vn;
            DataContext = vn;
            var cvnItems = StaticHelpers.LocalDatabase.CharacterVNs.Where(cvn => cvn.ListedVNId == vn.VNID).ToArray();
            CharacterTiles.ItemsSource = cvnItems.Select(CharacterTile.FromCharacterVN);
        }

        private async void VNPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.GetRelationsAnimeScreens();
            RelationsCombobox.SelectedIndex = 0;
            AnimeCombobox.SelectedIndex = 0;
        }

        private void ScrollViewer_OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scrollviewer = (ScrollViewer)sender;
            scrollviewer.CanContentScroll = true;
            if (e.Delta > 0) scrollviewer.LineLeft();
            else scrollviewer.LineRight();
            e.Handled = true;
        }

        private void ID_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start($"https://vndb.org/v{_viewModel.VNID}");
        }
    }
}
