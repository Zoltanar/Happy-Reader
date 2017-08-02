using System;
using System.Runtime.InteropServices;
using System.Windows;
// ReSharper disable InconsistentNaming

namespace Happy_Reader.WinForms
{
    static class Win32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public readonly int X;
            public readonly int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
            public POINT(Point pt)
            {
                X = Convert.ToInt32(pt.X);
                Y = Convert.ToInt32(pt.Y);
            }
        };

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    };
}
