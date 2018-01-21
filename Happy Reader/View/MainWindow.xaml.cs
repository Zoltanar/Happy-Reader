using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
using Microsoft.Win32;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolTipIcon = System.Windows.Forms.ToolTipIcon;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly NotifyIcon _trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel(this);
            DataContext = _viewModel;
            // ReSharper disable once PossibleNullReferenceException
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/logo-hr.ico")).Stream;
            _trayIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon(iconStream),
                Visible = true
            };
            _trayIcon.DoubleClick += delegate
            {
                Show();
                WindowState = WindowState.Normal;
            };
            _viewModel.NotificationEvent += ShowNotification;
            Log.NotificationEvent += ShowNotification;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized) Hide();
            base.OnStateChanged(e);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Stopwatch watch = Stopwatch.StartNew();
            _viewModel.ClipboardManager = new ClipboardManager(this);
            await _viewModel.Initialize(watch);
        }

        private void AddEntry_Click(object sender, RoutedEventArgs e)
        {
            AddEntry();
        }
        
        private void DropFileOnGamesTab(object sender, DragEventArgs e)
        {
            string file = (e.Data.GetData(DataFormats.FileDrop) as string[])?.First();
            if (string.IsNullOrWhiteSpace(file)) return;
            var ext = Path.GetExtension(file);
            if (!ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                GameResponseLabel.Content = "Dragged file isn't an executable.";
                return;
            }
            GameResponseLabel.Content = $"Dragged file was {Path.GetFileName(file)}";
            var titledImage = _viewModel.AddGameFile(file);
            if (titledImage == null) return;
            ((IList<UserGameTile>)GameFiles.ItemsSource).Add(titledImage);
        }

        private void GameFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = GameFiles.SelectedItem as UserGameTile;
            var userGame = (UserGame)item?.DataContext;
            if (userGame == null) return;
            _viewModel.HookUserGame(userGame);
        }
        
        private void Debug_Button(object sender, RoutedEventArgs e)
        {
            _viewModel.DebugButton();
        }


        public void ShowNotification(object sender, [NotNull]string message, string title = "Notification")
        {
            Console.WriteLine($"Log - {title} - {message}");
            _trayIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
        }

        private void RemoveUserGame(object sender, RoutedEventArgs e)
        {
            if (GameFiles.SelectedItems.Count != 1)
            {
                GameResponseLabel.Content = "You must select 1 item.";
                return;
            }
            if (!StaticMethods.UserIsSure()) return;
            _viewModel.RemoveUserGame((UserGameTile)GameFiles.SelectedItems[0]);
        }
        
        public void TabMiddleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return;
            if (!(sender is TabItem tab)) return;
            MainTabControl.Items.Remove(tab);
        }

        public void AddEntryFromOutputWindow(string text) => AddEntry(text);

        private void AddEntry(string initialInput = "")
        {
            var tabItem = new TabItem
            {
                Header = "Add Entry",
                Name = "AddEntryControl",
                Content = new AddEntryControl(_viewModel, initialInput)
            };
            AddTabItem(tabItem);
        }

        public void OpenVNPanel(ListedVN vn)
        {
            var tabItem = new TabItem
            {
                Header = StaticHelpers.TruncateString(vn.Title,30),
                Name = "VNPanel",
                Content = new VNPanel(vn)
            };
            AddTabItem(tabItem);
        }

        public void AddTabItem(TabItem tabItem)
        {
            tabItem.MouseUp += TabMiddleClick;
            MainTabControl.Items.Add(tabItem);
            MainTabControl.SelectedItem = tabItem;
        }

        private void ChangeIthPath(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            string directory = Path.GetDirectoryName(StaticHelpers.GSettings.IthPath);
            while (!Directory.Exists(directory))
            {
                directory = directory == null ? Environment.CurrentDirectory : Directory.GetParent(directory).FullName;
            }
            dialog.InitialDirectory = directory;
            var result = dialog.ShowDialog();
            if (result ?? false)
            {
                IthPathBox.Text = dialog.FileName;
                StaticHelpers.GSettings.IthPath = dialog.FileName;
            }
        }

        private void SetClipboardSize(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            StaticHelpers.GSettings.MaxClipboardSize = (int)((Slider)e.Source).Value;
        }
    }
}

