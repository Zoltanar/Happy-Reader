using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using IthVnrSharpLib;

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
			Process[] runningProcesses = { };
			try
			{
				runningProcesses = Process.GetProcesses();
				foreach (var process in runningProcesses)
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
							processInfo = new ProcessInfo(process, false,false);
						}
						processList.Add(processInfo);
					}
					catch (Exception ex)
					{
						Happy_Apps_Core.StaticHelpers.Logger.ToDebug($"isWow64: {isWow64}, result: {result}, {ex.Message}");
					}
				}
			}
			finally
			{
				foreach (var process in runningProcesses) process.Dispose();
			}

			ProcessGrid.ItemsSource = processList;
		}

		private void AttachProcess(object sender, RoutedEventArgs e)
		{
			if (!(ProcessGrid.SelectedItem is ProcessInfo item)) return; //todo return error
			uint pid = (uint)item.Id;
			var result = _ithViewModel.VnrProxy.Host_InjectByPID(pid);
			if (result)
			{
				var result2 = _ithViewModel.VnrProxy.Host_HijackProcess(pid);
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
