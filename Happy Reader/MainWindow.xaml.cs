using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /*var wih = new WindowInteropHelper(this);
            //var hWndSource = HwndSource.FromHwnd(wih.Handle);
            //var source = hWndSource;
            if (source != null)
            {
                source.AddHook(WinProc); // start processing window messages
                _hWndNextViewer = Win32.SetClipboardViewer(source.Handle); // set this window as a viewer
            }*/
            SaveSettings(null, null);
            /*var outputForm = new OutputWindow();
            outputForm.Show();
            outputForm.SetTextDebug();*/
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
    }
}

