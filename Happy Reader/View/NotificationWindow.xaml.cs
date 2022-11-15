using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Threading;
using Happy_Apps_Core;
using Happy_Reader.Database;

namespace Happy_Reader.View
{
    public partial class NotificationWindow : Window
    {
        private static NotificationWindow _currentWindow;
        private readonly bool _showInWindow;
        private NotificationWindow(string title, Block message, bool showInWindow)
        {
            InitializeComponent();
            TitleLabel.Content = title;
            TextBox.Document.Blocks.Clear();
            TextBox.Document.Blocks.Add(message);
            Topmost = true;
            _showInWindow = showInWindow;
        }
        
        public static void Launch(Log log)
        {
            var title = log.Kind.GetDescription();
            Launch(title, () =>
            {
                var p = new Paragraph();
                foreach (var inline in log.GetInlines()) p.Inlines.Add(inline);
                return p;
            }, false);
        }

        public static void Launch(string title, string message, bool inWindow)
        {
            Launch(title, () => new Paragraph(new Run(message)), inWindow);
        }

        private static void Launch(string title, Func<Paragraph> getMessage, bool inWindow)
        {
            Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (_currentWindow != null)
                {
                    _currentWindow.Close();
                    _currentWindow = null;
                }
                _currentWindow = new NotificationWindow(title, getMessage(), inWindow);
                _currentWindow.Show();
            });
        }

        private void Timeline_OnCompleted(object sender, EventArgs e)
        {
            _currentWindow = null;
            Close();
        }

        private void NotificationWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.Assert(Dispatcher != null, nameof(Dispatcher) + " != null");
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                var window = Application.Current.MainWindow;
                if (_showInWindow && window.WindowState != WindowState.Maximized)
                {
                    Left = window!.Left + window.Width - (ActualWidth);
                    Top = window.Top + window.Height - ActualHeight;
                    return;
                }
                var helper = new WindowInteropHelper(window);
                var screen = System.Windows.Forms.Screen.FromHandle(helper.Handle);
                var workingArea = screen.WorkingArea;
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget == null) return;
                var transform = source.CompositionTarget.TransformFromDevice;
                // ReSharper restore PossibleNullReferenceException
                var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));
                Left = corner.X - (ActualWidth + 100);
                Top = corner.Y - ActualHeight;
            }));
        }

        private void CloseClick(object sender, RoutedEventArgs e)=> Close();
    }
}
