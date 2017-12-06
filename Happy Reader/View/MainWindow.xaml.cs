using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.Database;
using Happy_Reader.Interop;
using JetBrains.Annotations;
using static Happy_Reader.StaticMethods;
using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace Happy_Reader
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
            UserTb.Text = "zolty";
            GameTb.Text = "Ikusa Megami VERITA";
            _viewModel = (MainWindowViewModel)DataContext;
            // ReSharper disable once PossibleNullReferenceException
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/logo-copy.ico")).Stream;
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
            await _viewModel.Loaded();
#if DEBUG
            _viewModel.ClipboardManager = new ClipboardManager(this);
#endif
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            var result = _viewModel.TrySaveSettings(UserTb.Text, GameTb.Text, out string response);
            SaveSettingsReplyLabel.Foreground = result ? Brushes.Black : Brushes.Red;
            SaveSettingsReplyLabel.Content = response;
        }

        private void DebugInject_Click(object sender, RoutedEventArgs e)
        {
            //var exeName = @"G:\VN\rootnuko\Tenioha\tenioha.exe";
            var exeName = @"G:\VN\Ryobo\凌母.exe";
            var process = StartProcess(exeName);
            if (process == null) throw new Exception("Process was not started.");
            process.WaitForInputIdle(5000);
            _viewModel.Hook(process);
        }


        private void AddUserhook(object sender, RoutedEventArgs e)
        {
            UserHook userHook = UserHook.fromCode("/HBC*0@416130");
            TextHook.instance.addUserHook(userHook);
        }
        private void BanProcess(object sender, RoutedEventArgs e)
        {
            var processName = ((ProcessCb.SelectedItem as ComboBoxItem)?.Tag as Process)?.ProcessName;
            if (string.IsNullOrWhiteSpace(processName)) return;
            _viewModel.BanProcess(processName);
        }

        private void HookToProcess(object sender, RoutedEventArgs e)
        {
            var process = (ProcessCb.SelectedItem as ComboBoxItem)?.Tag as Process;
            if (process == null) return;
            _viewModel.Hook(process);
        }

        private void AddEntry_Click(object sender, RoutedEventArgs e)
        {
            var tabItem = new TabItem
            {
                Header = "Add Entry",
                Name = "AddEntryControl",
                Content = new AddEntryControl(_viewModel)
            };
            MainTabControl.Items.Add(tabItem);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Closing();
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
            ((IList<TitledImage>)GameFiles.ItemsSource).Add(titledImage);
        }

        private void GameFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = GameFiles.SelectedItem as TitledImage;
            var userGame = (UserGame)item?.DataContext;
            if (userGame == null) return;
            var process = StartProcess(userGame.FilePath);
            if (userGame.ProcessName == null)
            {
                var game = Data.UserGames.Single(x => x.Id == userGame.Id);
                game.ProcessName = process.ProcessName;
                Data.SaveChanges();
            }
            if (userGame.HookProcess ?? false) _viewModel.HookV2(process, userGame);
        }

        private void Debug_Button(object sender, RoutedEventArgs e)
        {
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
            if (!UserIsSure()) return;
            _viewModel.RemoveUserGame((TitledImage)GameFiles.SelectedItems[0]);
        }

        private void TestTranslationClick(object sender, RoutedEventArgs e)
        {
            _viewModel.TestTranslation();
        }
    }
}

