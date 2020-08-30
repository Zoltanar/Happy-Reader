using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace Happy_Reader
{
	public static class WinAPI
	{
		public delegate void WinEventDelegate(
			IntPtr hWinEventHook,
			uint eventType,
			IntPtr hWnd,
			int idObject,
			int idChild,
			uint dwEventThread,
			uint dwmsEventTime);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsIconic(IntPtr hWnd);

		[DllImport("User32.dll", SetLastError = true)]
		public static extern IntPtr SetWinEventHook(
			uint eventMin,
			uint eventMax,
			IntPtr hmodWinEventProc,
			WinEventDelegate lpfnWinEventProc,
			uint idProcess,
			uint idThread,
			uint dwFlags);


		[DllImport("user32.dll")]
		public static extern bool UnhookWinEvent(
			IntPtr hWinEventHook
		);

		private enum SystemEvents : uint
		{
			EVENT_MIN = 0x00000001,
			EVENT_SYSTEM_MOVESIZESTART = 0x000A,
			EVENT_SYSTEM_MOVESIZEEND = 0x000B,
			EVENT_SYSTEM_MINIMIZESTART = 0x0016,
			EVENT_SYSTEM_MINIMIZEEND = 0x0017,
			EVENT_MAX = 0x7FFFFFFF
		}

		private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

		public sealed class WindowHook : IDisposable
		{
			public delegate void Win32Event(IntPtr hWnd);


			public Win32Event OnWindowMinimizeEnd;
			public Win32Event OnWindowMinimizeStart;
			public Win32Event OnWindowMoveSizeEnd;
			public Win32Event OnWindowMoveSizeStart;

			private IntPtr _hookPointer;
			private bool _disposed;
			// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable keep object alive
			private WinEventDelegate _handleEvent;
			private IntPtr _mainWindowHandle;



			public WindowHook(Process process)
			{
				_mainWindowHandle = process.MainWindowHandle;
				_handleEvent = WinEvent;
				_hookPointer = SetWinEventHook((uint)SystemEvents.EVENT_SYSTEM_MOVESIZESTART,
																(uint)SystemEvents.EVENT_SYSTEM_MINIMIZEEND,
																_mainWindowHandle,
																_handleEvent,
																(uint)process.Id,
																0, WINEVENT_OUTOFCONTEXT
						);
				if (IntPtr.Zero.Equals(_hookPointer))
					throw new Win32Exception();
			}

			public void Dispose()
			{
				Dispose(true);
			}


			private void WinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
			{
				switch ((SystemEvents)eventType)
				{
					case SystemEvents.EVENT_SYSTEM_MINIMIZESTART:
						OnWindowMinimizeStart?.Invoke(hWnd);
						break;
					case SystemEvents.EVENT_SYSTEM_MINIMIZEEND:
						OnWindowMinimizeEnd?.Invoke(hWnd);
						break;
					case SystemEvents.EVENT_SYSTEM_MOVESIZESTART:
						OnWindowMoveSizeStart?.Invoke(hWnd);
						break;
					case SystemEvents.EVENT_SYSTEM_MOVESIZEEND:
						OnWindowMoveSizeEnd?.Invoke(hWnd);
						break;
				}
			}


			~WindowHook() => Dispose(false);

			private void Dispose(bool manual)
			{
				if (_disposed) return;
				if (!IntPtr.Zero.Equals(_hookPointer)) UnhookWinEvent(_hookPointer);
				_hookPointer = IntPtr.Zero;
				_mainWindowHandle = IntPtr.Zero;
				_handleEvent = null;
				OnWindowMinimizeStart = null;
				OnWindowMinimizeEnd = null;
				OnWindowMoveSizeStart = null;
				OnWindowMoveSizeEnd = null;
				_disposed = true;
				if (manual) GC.SuppressFinalize(this);
			}
		}
	}
}
