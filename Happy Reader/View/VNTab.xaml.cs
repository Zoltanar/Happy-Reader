using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for VNTab.xaml
    /// </summary>
    public partial class VNTab : UserControl
    {
        private VNTabViewModel _viewModel;
        private MainWindow _mainWindow;

        public VNTab()
        {
            InitializeComponent();
        }


        private async void UpdateURT(object sender, RoutedEventArgs e) => await _viewModel.UpdateURT();

        private async void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!(e.VerticalChange > 0)) return;
            var loc = e.VerticalOffset + e.ViewportHeight;
            if (loc+1 >= e.ExtentHeight) await _viewModel.AddListedVNPage();
        }

        private async void ShowAll(object sender, RoutedEventArgs e) => await _viewModel.RefreshListedVns(true);

        private async void SearchForVN(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            await _viewModel.SearchForVN(((TextBox)sender).Text);
        }

        private void VNTab_OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (VNTabViewModel)DataContext;
            _mainWindow = (MainWindow)Window.GetWindow(this);
        }

        private void VNTileDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            var item = VisualNovelItems.SelectedItem as VNTile;
            var vn = (ListedVN)item?.DataContext;
            if (vn == null) return;
            _mainWindow.OpenVNPanel(vn);
        }

        private async void ShowURT(object sender, RoutedEventArgs e) => await _viewModel.ShowURT();
    }
}
