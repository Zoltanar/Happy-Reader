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

		public static VNTile FromListedVN(ListedVN vn)
		{
			return new VNTile(vn);
		}

		private void ContextMenuOpened(object sender, RoutedEventArgs e) => VnMenu.ContextMenuOpened();

		private void VNTile_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			VnMenu.TransferItems(VnMenuParent);
			_loaded = true;
		}
	}
}
