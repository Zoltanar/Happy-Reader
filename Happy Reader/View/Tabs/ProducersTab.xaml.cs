using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	/// <summary>
	/// Interaction logic for ProducersTab.xaml
	/// </summary>
	public partial class ProducersTab : UserControl
	{
		private ProducersTabViewModel _viewModel;

		public ProducersTab()
		{
			InitializeComponent();
		}

		private void ProducersTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			_viewModel = (ProducersTabViewModel)DataContext;
		}

		private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (!(e.VerticalChange > 0)) return;
			var loc = e.VerticalOffset + e.ViewportHeight * 2;
			if (loc < e.ExtentHeight) return;
			_viewModel.AddListedProducersPage();
			((ScrollViewer)e.OriginalSource).ScrollToVerticalOffset(loc);
		}

		private async void SearchForProducer(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			await _viewModel.SearchForProducer(((TextBox)sender).Text);
		}

		private async void ShowAll(object sender, RoutedEventArgs e) => await _viewModel.RefreshListedProducers(true);

	}

}
