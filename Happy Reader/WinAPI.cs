using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;
// ReSharper disable CommentTypo
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
namespace Happy_Reader
{
	public static class WinAPI
	{
		private static HookProcedure _processMouseHook;

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
		public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

		/// <summary>
		///     The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain.
		///     You would install a hook procedure to monitor the system for certain types of events. These events
		///     are associated either with a specific thread or with all threads in the same desktop as the calling thread.
		/// </summary>
		/// <param name="idHook">
		///     [in] Specifies the type of hook procedure to be installed. This parameter can be one of the following values.
		/// </param>
		/// <param name="lpfn">
		///     [in] Pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a
		///     thread created by a different process, the lpfn parameter must point to a hook procedure in a dynamic-link
		///     library (DLL). Otherwise, lpfn can point to a hook procedure in the code associated with the current process.
		/// </param>
		/// <param name="hMod">
		///     [in] Handle to the DLL containing the hook procedure pointed to by the lpfn parameter.
		///     The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by
		///     the current process and if the hook procedure is within the code associated with the current process.
		/// </param>
		/// <param name="dwThreadId">
		///     [in] Specifies the identifier of the thread with which the hook procedure is to be associated.
		///     If this parameter is zero, the hook procedure is associated with all existing threads running in the
		///     same desktop as the calling thread.
		/// </param>
		/// <returns>
		///     If the function succeeds, the return value is the handle to the hook procedure.
		///     If the function fails, the return value is NULL. To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
		/// </remarks>
		[DllImport("user32.dll", CharSet = CharSet.Auto,
				CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern HookProcedureHandle SetWindowsHookEx(
				int idHook,
				HookProcedure lpfn,
				IntPtr hMod,
				int dwThreadId);

		/// <summary>
		///     The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx
		///     function.
		/// </summary>
		/// <param name="idHook">
		///     [in] Handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to
		///     SetWindowsHookEx.
		/// </param>
		/// <returns>
		///     If the function succeeds, the return value is nonzero.
		///     If the function fails, the return value is zero. To get extended error information, call GetLastError.
		/// </returns>
		/// <remarks>
		///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
		/// </remarks>
		[DllImport("user32.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern int UnhookWindowsHookEx(IntPtr idHook);

		[DllImport("user32.dll", SetLastError = true)]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		public static HookProcedureHandle HookMouseEvents(HookMouseCallback globalMouseClick)
		{
			_processMouseHook = (code, param, lParam) => HookProcedure2(code, param, lParam, globalMouseClick);
			
				var hookHandle = SetWindowsHookEx(
					WinAPIConstants.WH_MOUSE_LL,
					_processMouseHook,
					IntPtr.Zero,
					0);
				return hookHandle;
		}

		private static IntPtr HookProcedure2(int nCode, IntPtr wParam, IntPtr lParam, HookMouseCallback callback)
		{
			var passThrough = nCode != 0;
			if (passThrough) return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
			var args = MouseEventExtArgs.FromParams(wParam, lParam);
			var continueProcessing = callback(args);
			return !continueProcessing ? new IntPtr(-1) : CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
		}
		
		/// <summary>
		/// The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain.
		/// A hook procedure can call this function either before or after processing the hook information.
		/// </summary>
		/// <param name="idHook">This parameter is ignored.</param>
		/// <param name="nCode">[in] Specifies the hook code passed to the current hook procedure.</param>
		/// <param name="wParam">[in] Specifies the wParam value passed to the current hook procedure.</param>
		/// <param name="lParam">[in] Specifies the lParam value passed to the current hook procedure.</param>
		/// <returns>This value is returned by the next hook procedure in the chain.</returns>
		/// <remarks>
		///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
		/// </remarks>
		[DllImport("user32.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.StdCall)]
		internal static extern IntPtr CallNextHookEx(
			IntPtr idHook,
			int nCode,
			IntPtr wParam,
			IntPtr lParam);

		public delegate void WinEventDelegate(
			IntPtr hWinEventHook,
			uint eventType,
			IntPtr hWnd,
			int idObject,
			int idChild,
			uint dwEventThread,
			uint dwmsEventTime);

		public delegate IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam);

		public delegate bool HookMouseCallback(MouseEventExtArgs args);

		[UsedImplicitly]
		public class HookProcedureHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			private static bool _closing;

			static HookProcedureHandle()
			{
				Application.Current.Exit += (_, _) => { _closing = true; };
			}

			public HookProcedureHandle()
				: base(true)
			{
			}

			protected override bool ReleaseHandle()
			{
				//NOTE Calling Unhook during process exit causes delay
				if (_closing) return true;
				return UnhookWindowsHookEx(handle) != 0;
			}
		}
		
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
				_hookPointer = SetWinEventHook((uint)WinAPIConstants.Events.EVENT_SYSTEM_MOVESIZESTART,
																(uint)WinAPIConstants.Events.EVENT_SYSTEM_MINIMIZEEND,
																_mainWindowHandle,
																_handleEvent,
																(uint)process.Id,
																0, WinAPIConstants.WINEVENT_OUTOFCONTEXT
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
				switch ((WinAPIConstants.Events)eventType)
				{
					case WinAPIConstants.Events.EVENT_SYSTEM_MINIMIZESTART:
						OnWindowMinimizeStart?.Invoke(hWnd);
						break;
					case WinAPIConstants.Events.EVENT_SYSTEM_MINIMIZEEND:
						OnWindowMinimizeEnd?.Invoke(hWnd);
						break;
					case WinAPIConstants.Events.EVENT_SYSTEM_MOVESIZESTART:
						OnWindowMoveSizeStart?.Invoke(hWnd);
						break;
					case WinAPIConstants.Events.EVENT_SYSTEM_MOVESIZEEND:
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
