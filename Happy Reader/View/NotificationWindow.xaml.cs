using System;
using System.Windows;
using System.Windows.Threading;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        public NotificationWindow(string title, string message)
        {
            InitializeComponent();
            TitleRun.Text = title;
            MessageRun.Text = message;
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
				//the debug parts are for when debugger is stopped around here.
#if DEBUG
	            try
	            {
#endif
					var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
		            // ReSharper disable PossibleNullReferenceException
		            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
		            // ReSharper restore PossibleNullReferenceException
		            var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));
		            Left = corner.X - ActualWidth - 100;
		            Top = corner.Y - ActualHeight;
#if DEBUG
				}
	            catch
	            {
		            // ignored
	            }
#endif
			}));
        }

        private void Timeline_OnCompleted(object sender, EventArgs e) => Close();
    }
}
