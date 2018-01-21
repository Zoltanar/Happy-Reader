using System.Windows.Controls;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for CharacterTile.xaml
    /// </summary>
    public partial class CharacterTile : UserControl
    {
        private readonly CharacterItem _viewModel;
        public CharacterTile(CharacterItem character)
        {
            InitializeComponent();
            _viewModel = character;
            DataContext = character;
            TraitsCb.SelectedIndex = 0;
        }

        public static CharacterTile FromCharacterVN(CharacterVN cvn)
        {
            var character = cvn.CharacterItem;
            character.AttachedVN = cvn;
            return new CharacterTile(character);
        }

        private void ID_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start($"https://vndb.org/c{_viewModel.ID}");
        }
    }
}
