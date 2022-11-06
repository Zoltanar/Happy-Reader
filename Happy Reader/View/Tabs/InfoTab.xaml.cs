using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View.Tabs
{
    public partial class InfoTab : UserControl
    {
        private InformationViewModel _viewModel => (InformationViewModel)DataContext;
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

        private void ImportTranslations(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
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
            foreach (var file in logsToDelete.OrderBy(fi=>fi.LastWriteTime))
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
    }
}
