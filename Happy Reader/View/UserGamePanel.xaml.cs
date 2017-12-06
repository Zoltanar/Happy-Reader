using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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

        public UserGamePanel(UserGame game)
        {
            InitializeComponent();
            DataContext = game;
            _viewModel = game;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("explorer", Path.GetDirectoryName(((UserGame)DataContext).FilePath));
        }

        private void SaveUsedDefinedName(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            _viewModel.SaveUserDefinedName(DisplayNameBox.Text);
        }

        private void SaveVNID(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            _viewModel.SaveVNID(VNIDNameBox.Text.Length == 0 ? null : (int?)int.Parse(VNIDNameBox.Text));
        }

        private static readonly Regex DigitRegex = new Regex(@"\d");
        
        private void DigitsOnly(object sender, TextCompositionEventArgs e)
        {
            if (!DigitRegex.IsMatch(e.Text)) e.Handled = true;
        }
    }
}
