using System;
using System.Windows.Forms;
using System.Windows;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.ComponentModel;

namespace Happy_Reader.WinForms
{
    /// <summary>
    /// Displays a WinForms.WebBrowser control over a given placement target element in a WPF Window.
    /// Applies the opacity of the Window to the WebBrowser control.
    /// </summary>
    class WebBrowserOverlayWf
    {
        Window _owner;
        FrameworkElement _placementTarget;
        Form _form; // the top-level window holding the WebBrowser control
        WebBrowser _wb = new WebBrowser();

        public WebBrowser WebBrowser { get { return _wb; } }

        public WebBrowserOverlayWf(FrameworkElement placementTarget)
        {
            _placementTarget = placementTarget;
            Window owner = Window.GetWindow(placementTarget);
            Debug.Assert(owner != null);
            _owner = owner;

            _form = new Form();
            _form.Opacity = owner.Opacity;
            _form.ShowInTaskbar = false;
            _form.FormBorderStyle = FormBorderStyle.None;
            _wb.Dock = DockStyle.Fill;
            _form.Controls.Add(_wb);

            //owner.SizeChanged += delegate { OnSizeLocationChanged(); };
            owner.LocationChanged += delegate { OnSizeLocationChanged(); };
            _placementTarget.SizeChanged += delegate { OnSizeLocationChanged(); };

            if (owner.IsVisible)
                InitialShow();
            else
                owner.SourceInitialized += delegate
                {
                    InitialShow();
                };

            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(UIElement.OpacityProperty, typeof(Window));
            dpd.AddValueChanged(owner, delegate { _form.Opacity = _owner.Opacity; });

            _form.FormClosing += delegate { _owner.Close(); };
        }

        void InitialShow()
        {
            NativeWindow owner = new NativeWindow();
            var fromVisual = (HwndSource)PresentationSource.FromVisual(_owner);
            if (fromVisual == null) throw new NullReferenceException("PresentationSource.FromVisual(_owner) was null.");
            owner.AssignHandle(fromVisual.Handle);
            _form.Show(owner);
            owner.ReleaseHandle();
        }

        DispatcherOperation _repositionCallback;

        void OnSizeLocationChanged()
        {
            // To reduce flicker when transparency is applied without DWM composition, 
            // do resizing at lower priority.
            if (_repositionCallback == null)
                _repositionCallback = _owner.Dispatcher.BeginInvoke(Reposition, DispatcherPriority.Input);
        }

        void Reposition()
        {
            _repositionCallback = null;

            Point offset = _placementTarget.TranslatePoint(new Point(), _owner);
            Point size = new Point(_placementTarget.ActualWidth, _placementTarget.ActualHeight);
            HwndSource hwndSource = (HwndSource)PresentationSource.FromVisual(_owner);
            Debug.Assert(hwndSource != null, "hwndSource != null");
            CompositionTarget ct = hwndSource.CompositionTarget;
            Debug.Assert(ct != null, "ct != null");
            offset = ct.TransformToDevice.Transform(offset);
            size = ct.TransformToDevice.Transform(size);

            Win32.POINT screenLocation = new Win32.POINT(offset);
            Win32.ClientToScreen(hwndSource.Handle, ref screenLocation);
            Win32.POINT screenSize = new Win32.POINT(size);

            Win32.MoveWindow(_form.Handle, screenLocation.X, screenLocation.Y, screenSize.X, screenSize.Y, true);
            //_form.SetBounds(screenLocation.X, screenLocation.Y, screenSize.X, screenSize.Y);
            //_form.Update();
        }
    };
}
