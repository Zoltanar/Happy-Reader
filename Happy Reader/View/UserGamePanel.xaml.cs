using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.Database;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for UserGamePanel.xaml
    /// </summary>
    public partial class UserGamePanel : UserControl
    {
        private readonly UserGame _viewModel;
        private readonly TitledImage _parent;

        public UserGamePanel(UserGame game, TitledImage parent)
        {
            InitializeComponent();
            DataContext = game;
            _viewModel = game;
            _parent = parent;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("explorer", Path.GetDirectoryName(((UserGame)DataContext).FilePath));
        }

        private void SaveUsedDefinedName(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            _viewModel.SaveUserDefinedName(DisplayNameBox.Text);
            _parent.RefreshContext();
        }
    }
}
