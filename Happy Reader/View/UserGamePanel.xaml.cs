using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Happy_Reader.Database;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for UserGamePanel.xaml
    /// </summary>
    public partial class UserGamePanel : UserControl
    {
        public UserGamePanel(UserGame game)
        {
            InitializeComponent();
            DataContext = game;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("explorer",Path.GetDirectoryName(((UserGame) DataContext).FilePath));
        }
    }
}
