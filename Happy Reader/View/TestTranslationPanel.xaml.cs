using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for TestTranslationPanel.xaml
    /// </summary>
    public partial class TestTranslationPanel : UserControl
    {
        private TranslationTester _viewModel;

        public TestTranslationPanel()
        {
            InitializeComponent();
        }

        private void EnterOnTester(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var acBox = (AutoCompleteBox)sender;
            string item = (string)acBox.SelectedItem ?? acBox.Text;
            if (string.IsNullOrWhiteSpace(item)) return;
            if (_viewModel.SelectGame(item, out string outputText))
            {
                TesterGameValid.Content = "✔️";
                TesterGameValid.Foreground = Brushes.Green;
            }
            else
            {
                TesterGameValid.Content = "❌";
                TesterGameValid.Foreground = Brushes.Red;
            }
            acBox.Text = outputText;
        }

        private void TestTranslationClick(object sender, RoutedEventArgs e) => _viewModel.Test();

        private void TestTranslationPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
	        if (DesignerProperties.GetIsInDesignMode(this)) return;
            _viewModel = (TranslationTester)DataContext;
        }
    }

}
