using System.Windows;
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

        private async void VNPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            await ((ListedVN)DataContext).GetRelationsAnimeScreens();
            RelationsCombobox.SelectedIndex = 0;
            AnimeCombobox.SelectedIndex = 0;
            //ScreensCombobox.SelectedIndex = 0;
        }
    }
}
