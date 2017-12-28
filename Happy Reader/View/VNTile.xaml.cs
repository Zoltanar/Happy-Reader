using System.Windows.Controls;
using Happy_Apps_Core;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for VNTile.xaml
    /// </summary>
    public partial class VNTile : UserControl
    {
        public VNTile(ListedVN vn)
        {
            DataContext = vn;
            InitializeComponent();
        }
    }
}
