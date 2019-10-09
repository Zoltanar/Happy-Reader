using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader.View.Tiles
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
                var groups = character.DbTraits.Select(trait => 
								{
									var found = DumpFiles.PlainTraits.Find(x => x.ID == trait.TraitId);
									if(found == null) { }
									return found;
								}).GroupBy(x => x?.TopmostParentName ?? "Not found");
                foreach (var group in groups)
                {
                    linkList.Add(new Run($"{group.Key}: "));
                    foreach (var trait in group)
                    {
                        Inline content = new Run(trait.Name);

                        if(StaticHelpers.GSettings.AlertTraitIDs.Contains(trait.ID)) content = new Bold(content);
                        var link = new Hyperlink(content) { Tag = trait };
                        linkList.Add(link);
                    }
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
            Process.Start($"https://vndb.org/c{_viewModel.ID}");
        }
    }
}
