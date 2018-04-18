using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Happy_Reader.Database;

namespace Happy_Reader.View
{
	/// <summary>
	/// Interaction logic for NotificationWindow.xaml
	/// </summary>
	public partial class NotificationWindow : Window
	{
		private NotificationWindow(string title, Paragraph message)
		{
			InitializeComponent();
			TitleRun.Text = title;
			TextBox.Document.Blocks.Clear();
			TextBox.Document.Blocks.Add(message);
			Topmost = true;
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

		public static void Launch(Log log)
		{
			Application.Current.Dispatcher.BeginInvoke(() => new NotificationWindow(log.Kind.ToString(), log.GetParagraph()).Show());
		}
		public static void Launch(string title, string message)
		{
			Application.Current.Dispatcher.BeginInvoke(() => new NotificationWindow(title, new Paragraph(new Run(message))).Show());
		}

		private void Timeline_OnCompleted(object sender, EventArgs e) => Close();
	}
}
