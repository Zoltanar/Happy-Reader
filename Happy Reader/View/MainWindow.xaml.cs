using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.Interop;
using static Happy_Reader.StaticMethods;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {

            InitializeComponent();
            UserTb.Text = "zolty";
            GameTb.Text = "Ikusa Megami VERITA";
            _viewModel = (MainWindowViewModel)DataContext;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.Loaded();
            SaveSettings(null, null);
            GameResponseLabel.Content = "Finished loading.";
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
            if (MainTabControl.Items.Count > 2) return;
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
            ((IList<TitledImage>) GameFiles.ItemsSource).Add(titledImage);
        }

        private void GameFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = GameFiles.SelectedItem as TitledImage;
            var filePath = item?.FilePath;
            if (string.IsNullOrWhiteSpace(filePath)) return;
            var process = StartProcess(filePath);
            if (process == null) return;
            _viewModel.Hook(process);
        }
        
    }
}

