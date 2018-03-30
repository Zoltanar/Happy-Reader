using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using IthVnrSharpLib;
using static Happy_Reader.StaticMethods;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for Process_Explorer.xaml
    /// </summary>
    public partial class ProcessExplorer : Window
    {
        private readonly IthVnrViewModel _ithViewModel;

        public ProcessExplorer(IthVnrViewModel ithViewModel)
        {
            InitializeComponent();
            _ithViewModel = ithViewModel;
            RefreshProcessList();
        }

        private void RefreshProcessList()
        {
            var processList = new List<ProcessInfo>();
            foreach (var process in Process.GetProcesses())
            {

                bool isWow64 = false;
                bool result = false;
                try
                {
                    if (process.MainWindowHandle == IntPtr.Zero) continue;
                    result = NativeMethods.IsWow64Process(process.Handle, out isWow64);
                    if (!isWow64 || !result)
                    {
                        continue;
                    }

	                if (!_ithViewModel.HookManager.Processes.TryGetValue(process.Id, out var processInfo))
                    {
                        processInfo = new ProcessInfo(process, false);
                    }
                    processList.Add(processInfo);
                }
                catch (Win32Exception ex)
                {
                    Debug.WriteLine($"isWow64: {isWow64}, result: {result}, {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"isWow64: {isWow64}, result: {result}, {ex.Message}");
                }
            }
            ProcessGrid.ItemsSource = processList;
        }

        private void AttachProcess(object sender, RoutedEventArgs e)
        {
            if (!(ProcessGrid.SelectedItem is ProcessInfo item)) return; //todo return error
            uint pid = (uint)item.Process.Id;
            var result = VnrProxy.Host_InjectByPID(pid);
            if (result)
            {
                var result2 = VnrProxy.Host_HijackProcess(pid);
                if (!result2) { }
                //RefreshThreadWithPID(pid, true);
            }
            Close();
        }

        private void RefreshProcessList(object sender, RoutedEventArgs e)
        {
            RefreshProcessList();
        }
    }
}
