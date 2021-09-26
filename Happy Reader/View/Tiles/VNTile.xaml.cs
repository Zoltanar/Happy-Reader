using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core.Database;

namespace Happy_Reader.View.Tiles
{
	public partial class VNTile : UserControl
	{
		private VnMenuItem _vnMenu;
		private bool _loaded;

		public ListedVN VN { get; }

		private VnMenuItem VnMenu => _vnMenu ??= new VnMenuItem(VN);

		public VNTile(ListedVN vn)
		{
			InitializeComponent();
			DataContext = vn;
			VN = vn;
		}

		public static VNTile FromListedVN(ListedVN vn) => new(vn);

		private void ContextMenuOpened(object sender, RoutedEventArgs e) => VnMenu.ContextMenuOpened(false);

		private void VNTile_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			VnMenu.TransferItems(VnMenuParent);
			_loaded = true;
		}

		public void UpdateImageBinding()
		{
			var bindingExpression = CoverBox.GetBindingExpression(Image.SourceProperty);
			System.Diagnostics.Debug.Assert(bindingExpression != null, nameof(bindingExpression) + " != null");
			bindingExpression.UpdateTarget();
		}

		private void OpenProducerTab(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			StaticMethods.MainWindow.OpenProducerPanel(VN.Producer);
		}

		private void ID_OnClick(object sender, RoutedEventArgs e)
		{
			VnMenu.BrowseToVndbPage(sender,e);
		}
	}
}
