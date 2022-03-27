using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using IthVnrSharpLib;

namespace Happy_Reader.View
{
	public partial class ProcessExplorer : UserControl
	{
		private readonly IthVnrViewModel _ithViewModel;

		private Action Callback { get; }

		public ProcessExplorer(IthVnrViewModel ithViewModel, Action callback)
		{
			InitializeComponent();
			_ithViewModel = ithViewModel;
			Callback = callback;
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
							processInfo = new ProcessInfo(process, false, false);
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
			if (ProcessGrid.SelectedItem is not ProcessInfo item) return; //todo return error
			uint pid = (uint)item.Id;
			var initialised = _ithViewModel.InitialiseVnrHost();
			if (!initialised) return;
			var result = _ithViewModel.VnrHost.InjectIntoProcess(pid, out var errorMessage);
			if (!result) _ithViewModel.HookManager.ConsoleOutput($"Failed to inject process: {errorMessage}", true);
			Callback();
		}

		private void RefreshProcessList(object sender, RoutedEventArgs e)
		{
			RefreshProcessList();
		}

		private void OnOkClick(object sender, RoutedEventArgs e)
		{
			Callback();
		}
	}
}
