using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Happy_Apps_Core;
using Happy_Reader.Database;

namespace Happy_Reader.View
{
	public partial class NotificationWindow : Window
	{
		private NotificationWindow(string title, Block message)
		{
			InitializeComponent();
			TitleRun.Text = title;
			TextBox.Document.Blocks.Clear();
			TextBox.Document.Blocks.Add(message);
			Topmost = true;

		}

		public static NotificationWindow CurrentWindow;

		public static void Launch(Log log)
		{
			var title = log.Kind.GetDescription();
			Launch(title, () =>
			{
				var p = new Paragraph();
				foreach (var inline in log.GetInlines()) p.Inlines.Add(inline);
				return p;
			});
		}

		public static void Launch(string title, string message)
		{
			Launch(title, () => new Paragraph(new Run(message)));
		}

		public static void Launch(string title, Func<Paragraph> getMessage)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.BeginInvoke(() =>
			{
				if (CurrentWindow != null)
				{
					CurrentWindow.Close();
					CurrentWindow = null;
				}
				CurrentWindow = new NotificationWindow(title, getMessage());
				CurrentWindow.Show();
			});
		}

		private void Timeline_OnCompleted(object sender, EventArgs e)
		{
			CurrentWindow = null;
			Close();
		}

		private void NotificationWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			Debug.Assert(Dispatcher != null, nameof(Dispatcher) + " != null");
			Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
			{
				var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
				var source = PresentationSource.FromVisual(this);
				if (source?.CompositionTarget == null) return;
				var transform = source.CompositionTarget.TransformFromDevice;
				// ReSharper restore PossibleNullReferenceException
				var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));
				Left = corner.X - ActualWidth - 100;
				Top = corner.Y - ActualHeight;
			}));
		}
	}
}
