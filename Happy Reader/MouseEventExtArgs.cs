using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using IthVnrSharpLib.Properties;
// ReSharper disable All

namespace Happy_Reader
{
  /// <summary>
  ///     Provides extended data for the MouseClickExt and MouseMoveExt events.
  /// </summary>
  public class MouseEventExtArgs
  {
    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseEventExtArgs" /> class.
    /// </summary>
    /// <param name="button">One of the MouseButton values indicating which mouse button was pressed.</param>
    /// <param name="clicks">The number of times a mouse button was pressed.</param>
    /// <param name="point">The x and y coordinate of a mouse click, in pixels.</param>
    /// <param name="wheelDelta">A signed count of the number of dents the wheel has rotated.</param>
    /// <param name="timestamp">The system tick count when the event occurred.</param>
    /// <param name="isMouseButtonDown">True if event signals mouse button down.</param>
    /// <param name="isMouseButtonUp">True if event signals mouse button up.</param>
    internal MouseEventExtArgs(MouseButton button, int clicks, Point point, int wheelDelta, int timestamp,
        bool isMouseButtonDown, bool isMouseButtonUp)
    {
	    Button = button;
	    Clicks = clicks;
	    Point = point;
	    WheelDelta = wheelDelta;
      IsMouseButtonDown = isMouseButtonDown;
      IsMouseButtonUp = isMouseButtonUp;
      Timestamp = timestamp;
    }

    public MouseButton Button { get; set; }

    public int Clicks { get; set; }

    public int WheelDelta { get; set; }

    /// <summary>
    ///     Set this property to <b>true</b> inside your event handler to prevent further processing of the event in other
    ///     applications.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    ///     True if event contains information about wheel scroll.
    /// </summary>
    public bool WheelScrolled => WheelDelta != 0;

    /// <summary>
    ///     True if event signals a click. False if it was only a move or wheel scroll.
    /// </summary>
    public bool Clicked => Clicks > 0;

    /// <summary>
    ///     True if event signals mouse button down.
    /// </summary>
    public bool IsMouseButtonDown { get; }

    /// <summary>
    ///     True if event signals mouse button up.
    /// </summary>
    public bool IsMouseButtonUp { get; }

    /// <summary>
    ///     The system tick count of when the event occurred.
    /// </summary>
    public int Timestamp { get; }

    /// <summary>
    /// </summary>
    internal Point Point { get; set; }
    
    internal static MouseEventExtArgs FromParams(IntPtr wParam, IntPtr lParam)
    {
      var mouseStruct = (MouseStruct)Marshal.PtrToStructure(lParam, typeof(MouseStruct));
      return FromRawDataUniversal(wParam, mouseStruct);
    }

    /// <summary>
    ///     Creates <see cref="MouseEventExtArgs" /> from relevant mouse data.
    /// </summary>
    /// <param name="wParam">First Windows Message parameter.</param>
    /// <param name="mouseInfo">A MouseStruct containing information from which to construct MouseEventExtArgs.</param>
    /// <returns>A new MouseEventExtArgs object.</returns>
    private static MouseEventExtArgs FromRawDataUniversal(IntPtr wParam, MouseStruct mouseInfo)
    {
	    MouseButton button = 0;
      short mouseDelta = 0;
      var clickCount = 0;

      var isMouseButtonDown = false;
      var isMouseButtonUp = false;


      switch ((long)wParam)
      {
        case WinAPIConstants.WM_LBUTTONDOWN:
          isMouseButtonDown = true;
          button = MouseButton.Left;
          clickCount = 1;
          break;
        case WinAPIConstants.WM_LBUTTONUP:
          isMouseButtonUp = true;
          button = MouseButton.Left;
          clickCount = 1;
          break;
        case WinAPIConstants.WM_LBUTTONDBLCLK:
          isMouseButtonDown = true;
          button = MouseButton.Left;
          clickCount = 2;
          break;
        case WinAPIConstants.WM_RBUTTONDOWN:
          isMouseButtonDown = true;
          button = MouseButton.Right;
          clickCount = 1;
          break;
        case WinAPIConstants.WM_RBUTTONUP:
          isMouseButtonUp = true;
          button = MouseButton.Right;
          clickCount = 1;
          break;
        case WinAPIConstants.WM_RBUTTONDBLCLK:
          isMouseButtonDown = true;
          button = MouseButton.Right;
          clickCount = 2;
          break;
        case WinAPIConstants.WM_MBUTTONDOWN:
          isMouseButtonDown = true;
          button = MouseButton.Middle;
          clickCount = 1;
          break;
        case WinAPIConstants.WM_MBUTTONUP:
          isMouseButtonUp = true;
          button = MouseButton.Middle;
          clickCount = 1;
          break;
        case WinAPIConstants.WM_MBUTTONDBLCLK:
          isMouseButtonDown = true;
          button = MouseButton.Middle;
          clickCount = 2;
          break;
        case WinAPIConstants.WM_MOUSEWHEEL:
          mouseDelta = mouseInfo.MouseData;
          break;
        case WinAPIConstants.WM_XBUTTONDOWN:
          button = mouseInfo.MouseData == 1
              ? MouseButton.XButton1
              : MouseButton.XButton2;
          isMouseButtonDown = true;
          clickCount = 1;
          break;

        case WinAPIConstants.WM_XBUTTONUP:
          button = mouseInfo.MouseData == 1
              ? MouseButton.XButton1
              : MouseButton.XButton2;
          isMouseButtonUp = true;
          clickCount = 1;
          break;

        case WinAPIConstants.WM_XBUTTONDBLCLK:
          isMouseButtonDown = true;
          button = mouseInfo.MouseData == 1
              ? MouseButton.XButton1
              : MouseButton.XButton2;
          clickCount = 2;
          break;

        case WinAPIConstants.WM_MOUSEHWHEEL:
          mouseDelta = mouseInfo.MouseData;
          break;
      }

      var e = new MouseEventExtArgs(
          button,
          clickCount,
          mouseInfo.Point,
          mouseDelta,
          mouseInfo.Timestamp,
          isMouseButtonDown,
          isMouseButtonUp);

      return e;
    }
    
    [UsedImplicitly]
    public struct MouseStruct
    {
	    [UsedImplicitly]
      public Point Point;
      [UsedImplicitly]
      public short MouseData;
      [UsedImplicitly]
      public int Timestamp;
    }
  }
}
