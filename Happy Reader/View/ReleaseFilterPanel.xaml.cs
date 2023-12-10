using System.Windows.Controls;
using System.Windows.Data;
namespace Happy_Reader.View
{
    public partial class ReleaseFilterPanel : UserControl
    {
        private readonly ReleaseMonthFilter _releaseMonth = new();
        public IFilter ViewModel { get; set; }

        public ReleaseFilterPanel()
        {
            InitializeComponent();
            DataContext = _releaseMonth;
        }
        
        private void UpdateReleaseMonthFilter(object sender, DataTransferEventArgs e)
        {
            if (ViewModel == null) return;
            ViewModel.Value = _releaseMonth;
        }
    }
}
