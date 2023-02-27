using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Tabs;
using Happy_Reader.View.Tiles;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using Brushes = System.Windows.Media.Brushes;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Drawing.Image;

namespace Happy_Reader.View
{
    public partial class MainWindow
    {
        private bool _initialised;
        private bool _finalizing;
        private bool _finalized;
        private NotifyIcon _trayIcon;
        internal readonly SavedData SavedData = new();

        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            if (StaticHelpers.IsAlreadyRunningInstance() &&
                System.Windows.MessageBox.Show($"There is another instance of Happy Reader running currently.{Environment.NewLine}Do you wish to continue loading this instance?", "Happy Reader - Already Running", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                Close();
                Application.Current.Shutdown();
                return;
            }
            ViewModel = new MainWindowViewModel(ShowNotification);
            Log.NotificationEvent = ShowLogNotification;
            DataContext = ViewModel;
            CreateNotifyIcon();
        }

        private void CreateNotifyIcon()
        {
            var contextMenu = new ContextMenuStrip();
            EventHandler open = (_, _) =>
            {
                Show();
                Activate();
            };
            contextMenu.Items.Add(new ToolStripMenuItem("Open", null, open));
            contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, Exit));
            // ReSharper disable once PossibleNullReferenceException
            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/logo-hr.ico")).Stream;
            _trayIcon = new NotifyIcon
            {
                Icon = new Icon(iconStream),
                ContextMenuStrip = contextMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += open;
            _trayIcon.ContextMenuStrip.Opening += BuildTrayPopup;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this) || _initialised) return;
            
            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                ViewModel.ClipboardManager = new ClipboardManager(this);
                ViewModel.CaptureClipboardSettingChanged(
                    ViewModel.SettingsViewModel.TranslatorSettings.CaptureClipboard);
                var commandLineArgs = Environment.GetCommandLineArgs();
                var noEntries = commandLineArgs.Contains("-ne");
                var logVerbose = commandLineArgs.Contains("-lv");
                await ViewModel.Initialize(watch, !noEntries, logVerbose);
                //we call this here to create icons ahead of time.
                BuildTrayPopup(sender, e);
                UserGamesTabItem.GroupUserGames();
                LoadSavedData();
            }
            finally
            {
                _initialised = true;
            }
        }

        private void BuildTrayPopup(object sender, EventArgs e)
        {
            while (_trayIcon.ContextMenuStrip.Items.Count > 2) _trayIcon.ContextMenuStrip.Items.RemoveAt(_trayIcon.ContextMenuStrip.Items.Count - 3);
            int? added = null;
            foreach (var gameId in UserGame.LastGamesPlayed.Values.Reverse().Take(5))
            {
                var game = StaticMethods.Data.UserGames[gameId];
                if (game == null) continue;
                Image thumbnail;
                if (game.IconImageExists(out var icon))
                {
                    var thumbnailTemp = Image.FromFile(icon);
                    thumbnail = new Bitmap(thumbnailTemp);
                    thumbnailTemp.Dispose();
                }
                else
                {
                    game.SaveIconImage();
                    // ReSharper disable once PossibleNullReferenceException
                    thumbnail = game.IconImageExists(out icon) ? Image.FromFile(icon) : Image.FromStream(Application.GetResourceStream(new Uri(Theme.ImageNotFoundPath)).Stream);
                }
                var item = new ToolStripMenuItem(StaticHelpers.TruncateString30(game.DisplayName), thumbnail, LaunchGame) { Tag = game };
                if (!added.HasValue) added = 1;
                else added++;
                _trayIcon.ContextMenuStrip.Items.Insert(added.Value - 1, item);
            }
            if (added.HasValue) _trayIcon.ContextMenuStrip.Items.Insert(added.Value, new ToolStripSeparator());
        }

        private void LaunchGame(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not UserGame game) return;
            ViewModel.HookUserGame(game, null, null, false);
        }

        private void LoadSavedData()
        {
            if (!File.Exists(StaticMethods.SavedDataJson)) return;
            SavedData savedData;
            try
            {
                savedData = JsonConvert.DeserializeObject<SavedData>(File.ReadAllText(StaticMethods.SavedDataJson));
            }
            catch (Exception ex)
            {
                StaticHelpers.Logger.ToFile(ex);
                return;
            }
            if (savedData == null) return;
            foreach (var savedTab in savedData.Tabs)
            {
                switch (savedTab.TypeName)
                {
                    case nameof(VNTab):
                        {
                            var vn = StaticHelpers.LocalDatabase.VisualNovels[(int)savedTab.Id];
                            if (vn != null) OpenVNPanel(vn, false);
                            break;
                        }
                    case nameof(UserGameTab):
                        {
                            var userGame = StaticMethods.Data.UserGames[savedTab.Id];
                            if (userGame != null) OpenUserGamePanel(userGame, null, false);
                            break;
                        }
                    case nameof(ProducerTab):
                        {
                            var producer = StaticHelpers.LocalDatabase.Producers[(int)savedTab.Id];
                            if (producer != null) OpenProducerPanel(producer, false);
                            break;
                        }
                }
            }
        }

        public void ShowLogNotification([NotNull] Log message)
        {
            StaticHelpers.Logger.ToFile($"Notification - {message.Kind} - {message}");
            NotificationWindow.Launch(message);
        }

        public void ShowNotification(object sender, [NotNull] string message, string title, bool showInWindow)
        {
            StaticHelpers.Logger.ToFile($"Notification - {title} - {message}");
            NotificationWindow.Launch(title, message, showInWindow);
        }
        
        public void CreateAddEntriesTab(IEnumerable<Entry> entries)
        {
            var tabItem = new TabItem
            {
                Header = "Add Entries",
                Name = nameof(AddEntriesTab),
                Content = new AddEntriesTab(ViewModel, entries)
            };
            AddTabItem(tabItem, null, true);
        }

        public void OpenVNPanel(ListedVN vn, bool select = true)
        {
            var userGamesForVn = StaticMethods.Data.UserGames.Where(ug => ug.VNID == vn.VNID).ToList();
            //remove existing user game tabs if any
            foreach (var userGame in userGamesForVn)
            {
                var userGameTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == userGame);
                if (userGameTab != null) MainTabControl.Items.Remove(userGameTab);
            }
            var vnTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Content is VNTab vTab && vTab.ViewModel == vn);
            if (vnTab != null)
            {
                if (((VNTab)vnTab.Content).UserGames.Intersect(userGamesForVn).Count() == userGamesForVn.Count)
                {
                    //if tab already exists with same userGames
                    if (!select) return;
                    MainTabControl.SelectedItem = vnTab;
                    vnTab.Focus();
                    return;
                }
                //else, remove tab and re-create with new user games
                MainTabControl.Items.Remove(vnTab);
            }

            var hasSingleUserGame = userGamesForVn.Count == 1;
            var tabItem = new TabItem
            {
                Name = nameof(VNTab),
                Content = new VNTab(vn, userGamesForVn),
                DataContext = hasSingleUserGame ? userGamesForVn.First() : vn
            };
            var headerBinding = new Binding(hasSingleUserGame ? nameof(UserGame.DisplayName) : nameof(ListedVN.DisplayTitle))
            { Source = tabItem.DataContext };
            AddTabItem(tabItem, headerBinding, select);
        }

        public void OpenUserGamePanel(UserGame userGame, ListedVN priorVN, bool select = true)
        {
            if (priorVN != null)
            {
                var vnTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == priorVN);
                if (vnTab != null) MainTabControl.Items.Remove(vnTab);
            }
            var userGameTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == userGame);
            if (userGameTab != null)
            {
                if (!select) return;
                MainTabControl.SelectedItem = userGameTab;
                userGameTab.Focus();
                return;
            }
            var tabItem = new TabItem
            {
                Name = nameof(UserGameTab),
                Content = new UserGameTab(userGame, false),
                DataContext = userGame
            };
            var headerBinding = new Binding(nameof(UserGame.DisplayName))
            {
                Source = tabItem.DataContext
            };
            AddTabItem(tabItem, headerBinding, select);
        }

        public void OpenProducerPanel(ListedProducer producer, bool select = true)
        {
            var producerTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == producer);
            if (producerTab != null)
            {
                if (!select) return;
                MainTabControl.SelectedItem = producerTab;
                producerTab.Focus();
                return;
            }
            var tabItem = new TabItem
            {
                Name = nameof(ProducerTab),
                Content = new ProducerTab(producer),
                DataContext = producer
            };
            var headerBinding = new Binding(nameof(ListedProducer.Name))
            {
                Source = tabItem.DataContext
            };
            AddTabItem(tabItem, headerBinding, select);
        }

        private void AddTabItem(HeaderedContentControl tabItem, BindingBase headerBinding, bool select)
        {
            var background = StaticMethods.GetTabHeaderBackgroundAndSaveTab(tabItem.Content, SavedData.Tabs);
            tabItem.Tag = tabItem.DataContext;
            tabItem.HorizontalAlignment = HorizontalAlignment.Stretch;
            tabItem.VerticalAlignment = VerticalAlignment.Stretch;
            tabItem.Background = background;
            if(headerBinding != null) tabItem.SetBinding(HeaderedContentControl.HeaderProperty, headerBinding);
            MainTabControl.Items.Add(tabItem);
            ToggleCloseTabsButton(true);
            if (!select) return;
            MainTabControl.SelectedItem = tabItem;
            tabItem.Focus();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_finalized)
            {
                ViewModel?.ExitProcedures(this, null);
                SaveTabsToFile();
                return;
            }
            e.Cancel = true;
            Hide();
        }

        private void Exit(object sender, EventArgs e)
        {
            if (_finalizing || _finalized) return;
            _finalizing = true;
            Hide();
            _trayIcon.Visible = false;
            _finalized = true;
            Close();
        }

        private void SaveTabsToFile()
        {
            try
            {
                File.WriteAllText(StaticMethods.SavedDataJson, JsonConvert.SerializeObject(SavedData, Formatting.Indented));
            }
            catch (Exception ex)
            {
                StaticHelpers.Logger.ToFile(ex);
            }
        }

        public void SelectTab(Type viewModelType)
        {
            var tab = MainTabControl.Items.OfType<TabItem>().FirstOrDefault(t => ((FrameworkElement)t.Content).DataContext.GetType() == viewModelType);
            MainTabControl.SelectedItem = tab ?? throw new ArgumentNullException(nameof(tab), $"Did not find tab with ViewModel of type {viewModelType}");
        }

        private void ActiveGameTileLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not UserGameTile tile) return;
            tile.Row1.Height = new GridLength(0);
            tile.Row2.Height = new GridLength(1, GridUnitType.Star);
            tile.Row3.Height = new GridLength(0);
            tile.Row4.Height = new GridLength(0);
            tile.OuterBorder.Background = Brushes.Transparent;
            tile.OuterBorder.CornerRadius = new CornerRadius(0);
            tile.Mask.Background = Brushes.Transparent;
            tile.Mask.CornerRadius = new CornerRadius(0);
        }

        private void ShowSettingsClick(object sender, RoutedEventArgs e) => SelectTab(typeof(SettingsViewModel));

        private void CloseTabsClick(object sender, RoutedEventArgs e)
        {
            foreach (var tab in MainTabControl.Items.OfType<TabItem>().Reverse().ToList())
            {
                var content = tab.Content;
                switch (content)
                {
                    case VNTab:
                    case UserGameTab:
                    case ProducerTab:
                        MainTabControl.Items.Remove(tab);
                        break;
                }
            }
            ToggleCloseTabsButton(false);
        }

        /// <summary>
        /// Changes visibility of close tabs button.
        /// </summary>
        /// <param name="show">Whether button should be shown, if null, it will determine based on open tabs.</param>
        public void ToggleCloseTabsButton(bool? show)
        {
            show ??= MainTabControl.Items.OfType<TabItem>().Any(t => t.Content is VNTab or UserGameTab or ProducerTab);
            ApiStatusColumn.Width = new GridLength(show.Value ? 62 : 82);
            CloseTabButtonColumn.Width = new GridLength(show.Value ? 20 : 0);
        }

        private void OpenApiLogTab(object sender, RoutedEventArgs e)
        {
            var menuItem = (System.Windows.Controls.MenuItem)sender;
            if (menuItem.IsChecked) SelectTab(typeof(ApiLogViewModel));
            else SelectTab(typeof(UserGamesViewModel));
        }
    }
}