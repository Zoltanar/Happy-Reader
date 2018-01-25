using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
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
            var linkList = new List<Inline>();
            if (character.ID != 0 && character.DbTraits.Count > 0)
            {
                var groups = character.DbTraits.Select(trait => DumpFiles.PlainTraits.Find(x => x.ID == trait.TraitId))
                    .GroupBy(x => x.TopmostParentName);
                foreach (var group in groups)
                {
                    List<Inline> inlines = new List<Inline>();
                    inlines.Add(new Run($"{group.Key}: "));
                    inlines.AddRange(group.Select(x => new Hyperlink(new Run(x.Name)) { Tag = x }));
                    linkList.AddRange(inlines);
                }
            }
            TraitsControl.ItemsSource = linkList;
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
