using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using DatabaseDumpReader;
using Happy_Apps_Core;
using Happy_Apps_Core.Translation;
using static Happy_Apps_Core.StaticHelpers;
using SettingsViewModel = Happy_Reader.ViewModel.SettingsViewModel;

namespace Happy_Reader.View.Tabs
{
    public partial class SettingsTab : UserControl
    {
        private bool _loaded;
        private SettingsViewModel ViewModel => DataContext as SettingsViewModel ?? throw new ArgumentNullException($"Expected view model to be of type {nameof(SettingsViewModel)}");

        public SettingsTab() => InitializeComponent();

        private void SetMaxInputSize(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ViewModel.TranslatorSettings.MaxOutputSize = (int)((Slider)e.Source).Value;
        }

        private void LogInWithDetails(object sender, RoutedEventArgs e)
        {
            var response = Conn.Login(CSettings.ApiToken);
            LoginResponseBlock.Text = response;
        }

        private void OnNsfwToggle(object sender, RoutedEventArgs e)
        {
            //refresh images of active objects.
            StaticMethods.MainWindow.ViewModel.RefreshActiveObjectImages();
        }

        private void SettingsTab_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) || _loaded) return;
            OriginalColorSelector.TrySetColor();
            RomajiColorSelector.TrySetColor();
            TranslatedColorSelector.TrySetColor();
            ImageSyncCharacters.IsChecked = ViewModel.CoreSettings.SyncImages.HasFlag(ImageSyncMode.Characters);
            ImageSyncCovers.IsChecked = ViewModel.CoreSettings.SyncImages.HasFlag(ImageSyncMode.Covers);
            ImageSyncScreenshots.IsChecked = ViewModel.CoreSettings.SyncImages.HasFlag(ImageSyncMode.Screenshots);
            ImageSyncScreenshotThumbnails.IsChecked = ViewModel.CoreSettings.SyncImages.HasFlag(ImageSyncMode.Thumbnails);
            _loaded = true;
        }

        public void LoadTranslationPlugins(IEnumerable<ITranslator> translators)
        {
            bool anyAdded = false;
            foreach (var translator in translators)
            {
                if (translator.Properties.Count == 0) continue;
                var settingsPanel = new StackPanel() { Orientation = Orientation.Vertical };
                foreach (var property in translator.Properties) settingsPanel.Children.Add(GetPropertyControl(translator.SetProperty, property.Key, property.Value, translator.GetProperty(property.Key)));
                var header = $"{translator.SourceName} ({translator.Version})";
                var groupBox = new GroupBox { Header = header, Content = settingsPanel };
                PluginSettingsPanel.Children.Add(groupBox);
                anyAdded = true;
            }
            if (!anyAdded)
            {
                var label = new Label() { Content = "No translators found." };
                PluginSettingsPanel.Children.Add(label);
            }
        }

        private UIElement GetPropertyControl(Action<string, object> action, string header, Type type, object value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var tb = new TextBlock(new Run(header));
            tb.SetValue(Grid.ColumnProperty, 0);
            grid.Children.Add(tb);
            var valueControl = CreateControlForType(action, header, type, value);
            valueControl.SetValue(Grid.ColumnProperty, 1);
            grid.Children.Add(valueControl);
            return grid;
        }

        private FrameworkElement CreateControlForType(Action<string, object> action, string header, Type type, object value)
        {
            FrameworkElement control;
            if (type.IsEnum)
            {
                var enumArray = StaticMethods.GetEnumValues(type);
                var enumValues = enumArray.Select(e => e.Tag).ToList();
                var selectedIndex = enumValues.FindIndex(t => t.Equals(value));
                var cb = new ComboBox
                {
                    ItemsSource = enumArray,
                    SelectedValuePath = nameof(Tag)
                };
                cb.SelectedIndex = Math.Max(selectedIndex, 0);
                cb.SelectionChanged += (_, e) => action(header, e.AddedItems[0]);
                control = cb;
            }
            else
            {
                if (type == typeof(bool))
                {
                    var cb = new CheckBox { Content = header, IsChecked = value is true };
                    cb.Checked += (_, _) => action(header, true);
                    cb.Unchecked += (_, _) => action(header, false);
                    control = cb;
                }
                else if (type == typeof(string))
                {
                    var tb = new TextBox() { Text = value.ToString() };
                    tb.LostFocus += (s, _) => action(header, ((TextBox)s).Text);
                    tb.KeyUp += (s, ka) =>
                    {
                        if (ka.Key != Key.Enter) return;
                        action(header, ((TextBox)s).Text);
                    };
                    control = tb;
                }
                else if (type == typeof(int))
                {
                    var tb = new TextBox() { Text = value.ToString() };
                    tb.LostFocus += (s, _) => action(header, int.Parse(((TextBox)s).Text));
                    tb.KeyUp += (s, ka) =>
                    {
                        if (ka.Key != Key.Enter) return;
                        action(header, int.Parse(((TextBox)s).Text));
                    };
                    tb.PreviewTextInput += NumberValidationTextBox;
                    control = tb;
                }
                else control = new TextBlock(new Run($"Unsupported property type '{type.Name}'."));
            }
            return control;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OnDecimalVoteToggle(object sender, RoutedEventArgs e)
        {
            StaticMethods.MainWindow.ViewModel.RefreshActiveObjectUserVns();
        }

        private void ImageSyncChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            var set = ((CheckBox)sender).IsChecked ?? false;
            if (ReferenceEquals(sender, ImageSyncCharacters))
            {
                ViewModel.CoreSettings.SyncImages = set
                    ? ViewModel.CoreSettings.SyncImages |= ImageSyncMode.Characters
                    : ViewModel.CoreSettings.SyncImages &= ~ImageSyncMode.Characters;
            }
            if (ReferenceEquals(sender, ImageSyncCovers))
            {
                ViewModel.CoreSettings.SyncImages = set
                    ? ViewModel.CoreSettings.SyncImages |= ImageSyncMode.Covers
                    : ViewModel.CoreSettings.SyncImages &= ~ImageSyncMode.Covers;
            }
            if (ReferenceEquals(sender, ImageSyncScreenshots))
            {
                ViewModel.CoreSettings.SyncImages = set
                    ? ViewModel.CoreSettings.SyncImages |= ImageSyncMode.Screenshots
                    : ViewModel.CoreSettings.SyncImages &= ~ImageSyncMode.Screenshots;
            }
            if (ReferenceEquals(sender, ImageSyncScreenshotThumbnails))
            {
                ViewModel.CoreSettings.SyncImages = set
                    ? ViewModel.CoreSettings.SyncImages |= ImageSyncMode.Thumbnails
                    : ViewModel.CoreSettings.SyncImages &= ~ImageSyncMode.Thumbnails;
            }
        }

        private async void UpdateVndbData(object sender, RoutedEventArgs e)
        {
            var currentDatabaseState = LocalDatabase.Connection.State;
            if (currentDatabaseState != ConnectionState.Closed)
            {
                StaticMethods.MainWindow.ViewModel.StatusText = $"VNDB update was not started, current database was in {currentDatabaseState} state.";
                return;
            }
            var updated = Program.Execute(out var resultMessage);
            if (updated is Program.ExitCode.ReloadLatest or Program.ExitCode.Update)
            {
                await StaticMethods.MainWindow.ViewModel.DatabaseViewModel.Initialize();
            }
            StaticMethods.MainWindow.ViewModel.StatusText = $"VNDB Update: {updated} - {resultMessage}";
        }
    }
}
