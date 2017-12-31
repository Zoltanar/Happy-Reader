using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for VNTab.xaml
    /// </summary>
    public partial class VNTab : UserControl
    {
        private readonly VNTabViewModel _viewModel;

        public VNTab()
        {
            InitializeComponent();
            _viewModel = (VNTabViewModel)DataContext;
        }

        private async void UpdateURT(object sender, RoutedEventArgs e) => await _viewModel.UpdateURT();

        private async void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!(e.VerticalChange > 0)) return;
            var loc = e.VerticalOffset + e.ViewportHeight;
            if (loc+1 >= e.ExtentHeight) await _viewModel.AddListedVNPage();
        }

        private async void ResetURT(object sender, RoutedEventArgs e) => await _viewModel.RefreshListedVns(true);

        private async void SearchForVN(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            await _viewModel.SearchForVN(((TextBox)sender).Text);
        }

        public async Task Initialize(MainWindowViewModel mainViewModel) => await _viewModel.Initialize(mainViewModel); 
    }
}
