using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core.Translation;
using Happy_Reader.Database;
using Happy_Reader.Model;
using Happy_Reader.TranslationEngine;
using JetBrains.Annotations;

namespace Happy_Reader.View
{
    public partial class ImportTranslationsControl : UserControl
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public ICollection<ImportUserGame> UserGames { get; set; }
        public bool ImportVnTranslations { get; set; } = true;
        private Action Callback { get; }
        public List<IGrouping<int?, CachedTranslation>> VnTranslations { get; }
        public static List<EntryGame> AllEntryGames { get; set; }
        public List<ImportUserGame> UgTranslations { get; }

        public ImportTranslationsControl() => InitializeComponent();

        public ImportTranslationsControl(List<IGrouping<int?, CachedTranslation>> vnTranslations, List<ImportUserGame> ugTranslations, Action callback)
        {
            VnTranslations = vnTranslations;
            UgTranslations = ugTranslations;
            var allEntryGames = new List<EntryGame> { EntryGame.None };
            allEntryGames.AddRange(StaticMethods.Data.UserGames
                .Where(i => !i.VNID.HasValue)
                .Select(i => new EntryGame((int)i.Id, true, true))
                .Distinct());
            AllEntryGames = allEntryGames.ToList();
            Callback = callback;
            InitializeComponent();
            ImportUgLabel.Content = $"Import {UgTranslations.Sum(g => g.Translations.Count)} translations for {UgTranslations.Count} User Games";
            ImportVnCheckBox.Content = $"Import {VnTranslations.Sum(g => g.Count()):N0}  translations for {VnTranslations.Count} VNs";
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            if (ImportVnTranslations)
            {
                foreach (var translation in VnTranslations.SelectMany(g => g))
                {
                    StaticMethods.Data.Translations.UpsertLater(translation);
                }
            }
            foreach (var group in UgTranslations.Where(g=>g.IsSelected))
            {
                foreach(var translation in group.Translations)
                {
                    translation.GameId = group.SelectedGame.GameId;
                    translation.IsUserGame = group.SelectedGame.IsUserGame;
                    StaticMethods.Data.Translations.UpsertLater(translation);
                }
            }
            StaticMethods.Data.Translations.SaveChanges();
            Callback();
        }

        private void CancelClick(object sender, RoutedEventArgs e) => Callback();

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
