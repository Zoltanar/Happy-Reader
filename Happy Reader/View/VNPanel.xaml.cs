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
    }
}
