using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable All

namespace Happy_Reader
{
	public static class NativeMethods
	{
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
			public int Width => Right - Left;
			public int Height => Bottom - Top;
			public bool IsEmpty => Width == 0 && Height == 0;

			/// <summary>
			/// Moves rectangle by position (Left/Top) of <see cref="b"/>
			/// </summary>
			public RECT MovePosition(RECT b)
			{
				var rect = new RECT
				{
					Left = Left + b.Left,
					Top = Top + b.Top,
					Right =  Right + b.Left,
					Bottom =  Bottom + b.Top
				};
				return rect;
			}

			public RECT GetDifference(RECT b, bool keepSize)
			{
				var left = Left - b.Left;
				var top = Top - b.Top;
				var rect = new RECT
				{
					Left = left,
					Top = top,
					Right = keepSize ? left + Width : Width - b.Width + left,
					Bottom = keepSize ? top + Height : Height - b.Height + top
				};
				return rect;
			}
		}

		[DllImport("user32.dll")]
		public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		/// <summary>
		///     A clipboard viewer window receives the WM_CHANGECBCHAIN message when
		///     another window is removing itself from the clipboard viewer chain.
		/// </summary>
		internal const int WmChangecbchain = 0x030D;

		/// <summary>
		///     The WM_DRAWCLIPBOARD message notifies a clipboard viewer window that
		///     the content of the clipboard has changed.
		/// </summary>
		internal const int WmDrawclipboard = 0x0308;

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

		// See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx

		public const int WM_CLIPBOARDUPDATE = 0x031D;

		public static IntPtr HWND_MESSAGE = new IntPtr(-3);

		// See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AddClipboardFormatListener(IntPtr hwnd);

		[DllImport("user32.dll")]
		public static extern IntPtr GetClipboardOwner();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(
				[In] SafeHandleZeroOrMinusOneIsInvalid hProcess, [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
		);

		[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process([In] IntPtr processHandle, [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

		public static RECT GetWindowDimensions(Process process)
		{
			var windowHandle = process.MainWindowHandle;
			var ret = GetWindowRect(windowHandle, out var rct);
			if (rct.IsEmpty && !string.IsNullOrWhiteSpace(process.MainWindowTitle))
			{
				var hwnd = FindWindow(null, process.MainWindowTitle);
				GetWindowRect(hwnd, out rct);
			}
			return rct;
		}
	}
}
