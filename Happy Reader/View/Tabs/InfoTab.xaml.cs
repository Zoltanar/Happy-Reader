using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Translation;
using Happy_Reader.Database;
using Happy_Reader.Model;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace Happy_Reader.View.Tabs
{
    public partial class InfoTab : UserControl
    {
        private InformationViewModel _viewModel => (InformationViewModel)DataContext;
        private bool _showingInputControl;

        public InfoTab()
        {
            InitializeComponent();
        }

        private void DeleteOldCachedTranslations(object sender, RoutedEventArgs e) => ((InformationViewModel)DataContext).DeletedCachedTranslations(false);

        private void DeleteAllCachedTranslations(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Are you sure you want to delete all cached translations?", "Happy Reader - Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                ((InformationViewModel)DataContext).DeletedCachedTranslations(true);
            }
        }

        private void ExportCachedTranslations(object sender, RoutedEventArgs e) => _viewModel.ExportCachedTranslations();

        private void SetEntryGameEnter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            UpdateEntryGame(sender);
        }

        private void SetEntryGameLeftClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Released) return;
            UpdateEntryGame(sender);
        }

        private void UpdateEntryGame(object sender)
        {
            var acb = (AutoCompleteBox)sender;
            var binding = acb.GetBindingExpression(AutoCompleteBox.TextProperty);
            Debug.Assert(binding != null, nameof(binding) + " != null");
            binding.UpdateSource();
        }

        [UsedImplicitly]
        private bool EntryGameFilter(string input, object item)
        {
            //Short input is not filtered to prevent excessive loading times
            if (input.Length <= 4) return false;
            var gameData = (EntryGame)item;
            var result = gameData.ToString().ToLowerInvariant().Contains(input.ToLowerInvariant());
            return result;
        }

        private void OpenLogsFolder(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", $"\"{Happy_Apps_Core.StaticHelpers.LogsFolder}\"");
        }

        private void DeleteLogs(object sender, RoutedEventArgs e)
        {
            var logsFolder = new DirectoryInfo(Happy_Apps_Core.StaticHelpers.LogsFolder);
            var today = DateTime.Now.Date;
            var logsToDelete = logsFolder.GetFiles("*.log", SearchOption.TopDirectoryOnly).Where(fi => fi.LastWriteTime.Date < today);
            foreach (var file in logsToDelete.OrderBy(fi => fi.LastWriteTime))
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch (IOException)
                {
                    //ignore
                }
            }
            _viewModel.SetLogsSize();
        }

        private void ImportTranslations(object sender, RoutedEventArgs e)
        {
            if (_showingInputControl) return;
            var selectedTab = (TabItem)StaticMethods.MainWindow.MainTabControl.SelectedItem;
            var stackPanel = (StackPanel)((UserControl)selectedTab.Content).Content;
            var panelChildren = stackPanel.Children.Cast<UIElement>().ToArray();
            var importControl = ImportCachedTranslations(false, RestoreTab);
            if (importControl == null) return;
            _showingInputControl = true;
            stackPanel.Children.Clear();
            stackPanel.Children.Add(importControl);
            void RestoreTab()
            {
                try
                {
                    stackPanel.Children.Clear();
                    foreach (var gridChild in panelChildren)
                    {
                        stackPanel.Children.Add(gridChild);
                    }
                }
                finally
                {
                    _showingInputControl = false;
                }
            };
        }
                
        public ImportTranslationsControl ImportCachedTranslations(bool overwriteExisting, Action callback)
        {
            var dialog = new OpenFileDialog() { AddExtension = true, DefaultExt = ".sqlite" };
            var result = dialog.ShowDialog();
            if (result != true) return null;
            var import = new HappyReaderDatabase(dialog.FileName, true);
            var translations = import.Translations.ToList();
            if (!overwriteExisting) translations = translations.Where(t => StaticMethods.Data.Translations[t.Key] == null).ToList();
            if (!translations.Any())
            {
                //todo PrintReply($"No translations to import.");
                return null;
            }
            var vnTranslations = translations.Where(t => !t.IsUserGame && t.GameId.HasValue).GroupBy(t => t.GameId).ToList();
            var ugTranslations = ProcessUserGameTranslations(import.UserGames, translations);
            var panel = new ImportTranslationsControl(vnTranslations, ugTranslations, callback);
            return panel;
        }

        private List<ImportUserGame> ProcessUserGameTranslations(DACollection<long, UserGame> allImportGames, List<CachedTranslation> translations)
        {
            var allEntryGames1 = new List<EntryGame> { EntryGame.None };
            allEntryGames1.AddRange(StaticMethods.Data.UserGames
                .Where(i => !i.VNID.HasValue)
                .Select(i => new EntryGame((int)i.Id, true, true))
                .Distinct());
            var allEntryGames = allEntryGames1.ToArray();
            var importGames = translations
                .Where(t => t.IsUserGame && t.GameId.HasValue)
                .GroupBy(t => t.GameId)
                .Select(g => new ImportUserGame(allImportGames, g, StaticMethods.Data.UserGames, allEntryGames)).ToList();
            return importGames;
        }
    }
}
