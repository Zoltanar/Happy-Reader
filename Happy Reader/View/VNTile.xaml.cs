using System.Windows.Controls;
using Happy_Apps_Core;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for VNTile.xaml
    /// </summary>
    public partial class VNTile : UserControl
    {
        public VNTile(ListedVN vn)
        {
            InitializeComponent();
            DataContext = vn;
        }
    }
}
