using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
			int order = 1;
			ImageSource source;
			//todo move these flags to left side going down
			while ((source = StaticMethods.GetFlag(VN.LanguagesObject, order++, out var release)) != null)
			{
				var image = new Image { Source = source, MaxHeight = 12, MaxWidth = 24, Margin = new Thickness(3, 2, 3, 2) };
				if (!string.IsNullOrWhiteSpace(release.ReleaseDateString)) image.ToolTip = release.ReleaseDateString;
				var borderBrush = release.Mtl ? Theme.MtlBorder : Theme.NonMtlBorder;
				var grid = new Grid();
				var rectangle = new System.Windows.Shapes.Rectangle()
				{
					Stroke = borderBrush,
					StrokeThickness = 2,
					SnapsToDevicePixels = true
				};
				if (release.Partial) rectangle.StrokeDashArray = new DoubleCollection(new[] { 2d, 2d });
				grid.Children.Add(rectangle);
				grid.Children.Add(image);
				DockPanel.SetDock(grid, Dock.Right);
				LanguagesPanel.Children.Add(grid);
			}
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
			VnMenu.BrowseToVndbPage(sender, e);
		}
	}
}
