using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Happy_Reader.Model;
using Happy_Reader.View;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
	internal enum EncodingEnum
	{
		// ReSharper disable InconsistentNaming
		// ReSharper disable UnusedMember.Local
		ShiftJis,
		UTF8,
		Unicode
		// ReSharper restore UnusedMember.Local
		// ReSharper restore InconsistentNaming
	}

	public enum HookMode
	{
		None = 0,
		VnrHook = 1,
		VnrAgent = 2
	}

	public class GameHookSettings
	{
		private readonly UserGame _userGame;
		private NativeMethods.RECT? _locationOnMoveStart;
		private WinAPI.WindowHook _windowHook;
		private bool _removeRepetition;
		private EncodingEnum _prefEncodingEnum;
		private bool _mergeByHookCode;
		private bool _matchHookCode;
		private HookMode _hookProcess;
		private NativeMethods.RECT _outputRectangle;

		public GameHookSettings(UserGame userGame)
		{
			_userGame = userGame;
		}

		public static Encoding[] Encodings => IthVnrSharpLib.IthVnrViewModel.Encodings;
		public static HookMode[] HookModes { get; } = (HookMode[])Enum.GetValues(typeof(HookMode));

		public bool RemoveRepetition
		{
			get => _removeRepetition;
			set
			{
				if (_removeRepetition == value) return;
				_removeRepetition = value;
				if (_userGame.Loaded) _userGame.ReadyToUpsert = true;
			}
		}

		internal EncodingEnum PrefEncodingEnum
		{
			get => _prefEncodingEnum;
			set
			{
				if (_prefEncodingEnum == value) return;
				_prefEncodingEnum = value;
				if (_userGame.Loaded) _userGame.ReadyToUpsert = true;
			}
		}

		public bool MergeByHookCode
		{
			get => _mergeByHookCode;
			set
			{
				if (_mergeByHookCode == value) return;
				_mergeByHookCode = value;
				if (_userGame.Loaded) _userGame.ReadyToUpsert = true;
			}
		}

		public bool MatchHookCode
		{
			get => _matchHookCode;
			set
			{
				if (_matchHookCode == value) return;
				_matchHookCode = value;
				if (_userGame.Loaded) _userGame.ReadyToUpsert = true;
			}
		}

		/// <summary>
		/// Space separated string of hook codes.
		/// </summary>
		public string HookCodes { get; internal set; }

		public HookMode HookProcess
		{
			get => _hookProcess;
			set
			{
				if (_hookProcess == value) return;
				_hookProcess = value;
				if (_userGame.Loaded) _userGame.ReadyToUpsert = true;
			}
		}

		public bool IsHooked => _userGame.Process != null && HookProcess != HookMode.None;

		/// <summary>
		/// Stores position of output window, relative to game window.
		/// </summary>
		public NativeMethods.RECT OutputRectangle
		{
			get => _outputRectangle;
			internal set
			{
				if (_outputRectangle.Equals(value)) return;
				_outputRectangle = value;
				if (_userGame.Loaded) _userGame.ReadyToUpsert = true;
			}
		}

		public Encoding PrefEncoding
		{
			get => Encodings[(int)PrefEncodingEnum];
			set
			{
				var index = Array.IndexOf((Array) Encodings, value);
				PrefEncodingEnum = (EncodingEnum)index;
			}
		}

		public OutputWindow OutputWindow { get; set; }

		internal static NativeMethods.RECT GetOutputRectangle(string outputWindow)
		{
			if (string.IsNullOrEmpty(outputWindow)) return StaticMethods.OutputWindowStartPosition;
			try
			{
				//invariant culture required to ensure comma is not treated as decimal.
				var parts = outputWindow.Split(',').Select(i => int.Parse(i, CultureInfo.InvariantCulture)).ToList();
				var rect = new NativeMethods.RECT { Left = parts[0], Top = parts[1], Right = parts[2] + parts[0], Bottom = parts[3] + parts[1] };
				if (rect.Width < 0 || rect.Height < 0) return StaticMethods.OutputWindowStartPosition;
				return rect;
			}
			catch
			{
				return StaticMethods.OutputWindowStartPosition;
			}
		}

		public void SaveHookCode(string hookCode, bool addSingleCode)
		{
			if (!addSingleCode)
			{
				HookCodes = hookCode;
			}
			else
			{
				var hookCodes = (HookCodes ?? string.Empty).Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
				var newHookCode = string.IsNullOrWhiteSpace(hookCode) ? null : hookCode.Trim();
				if (newHookCode == null || hookCodes.Contains(newHookCode, StringComparer.OrdinalIgnoreCase)) return;
				HookCodes = string.Join(" ", hookCodes.Concat(new[] { newHookCode }));
			}
			StaticMethods.Data.UserGames.Upsert(_userGame, true);
			_userGame.OnPropertyChanged($"{nameof(UserGame.GameHookSettings)}");
		}

		private void WindowMoveStarts(IntPtr windowPointer)
		{
			if (OutputWindow == null) return;
			var success = NativeMethods.GetWindowRect(windowPointer, out var location);
			if (success) _locationOnMoveStart = location;
		}

		private void WindowMoveEnds(IntPtr windowPointer)
		{
			var success = NativeMethods.GetWindowRect(windowPointer, out var gameRectangle);
			if (!success || !_locationOnMoveStart.HasValue) return;
			var difference = gameRectangle.GetDifference(_locationOnMoveStart.Value, false);
			OutputWindow.MoveByDifference(difference);
			_locationOnMoveStart = null;
		}

		public void SaveOutputRectangle(NativeMethods.RECT absoluteRectangle)
		{
			var windowPointer = _userGame.Process.MainWindowHandle;
			var success = NativeMethods.GetWindowRect(windowPointer, out var gameRectangle);
			if (!success) return;
			var relativeRectangle = absoluteRectangle.GetDifference(gameRectangle, true);
			OutputRectangle = relativeRectangle;
		}

		private void WindowsIsRestored(IntPtr windowPointer)
		{
			StaticHelpers.Logger.ToDebug($"Restored {_userGame.DisplayName}, starting running time at {_userGame.RunningTime.Elapsed}");
			_userGame.RunningTime.Start();
			_userGame.OnPropertyChanged(nameof(_userGame.RunningStatus));
            if (StaticMethods.Settings.TranslatorSettings.MuteOnMinimise) VolumeMixer.SetApplicationMute(_userGame.Process.Id, false);
            if (OutputWindow?.InitialisedWindowLocation ?? false) OutputWindow.Show();
		}

		private void WindowIsMinimised(IntPtr windowPointer)
		{
			_userGame.RunningTime.Stop();
			StaticHelpers.Logger.ToDebug($"Minimized {_userGame.DisplayName}, stopped running time at {_userGame.RunningTime.Elapsed}");
			_userGame.OnPropertyChanged(nameof(_userGame.RunningStatus));
			_userGame.OnPropertyChanged(nameof(_userGame.TimeOpen));
			if (StaticMethods.Settings.TranslatorSettings.MuteOnMinimise) VolumeMixer.SetApplicationMute(_userGame.Process.Id, true);
			if (OutputWindow?.InitialisedWindowLocation ?? false) OutputWindow.Hide();
		}

		public void InitialiseWindow(Process process)
		{
			_windowHook = new WinAPI.WindowHook(process);
			_windowHook.OnWindowMinimizeStart += WindowIsMinimised;
			_windowHook.OnWindowMinimizeEnd += WindowsIsRestored;
			_windowHook.OnWindowMoveSizeStart += WindowMoveStarts;
			_windowHook.OnWindowMoveSizeEnd += WindowMoveEnds;
			if (WinAPI.IsIconic(process.MainWindowHandle)) WindowIsMinimised(process.MainWindowHandle);
		}

		public void DisposeWindow()
		{
			_windowHook?.Dispose();
			_windowHook = null;
		}
	}
}