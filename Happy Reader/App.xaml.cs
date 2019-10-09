using System;
using System.Windows.Threading;

namespace Happy_Reader
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		private bool _showingError;
		public App()
		{
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}
		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (_showingError)
			{
				e.Handled = true;
				return;
			}
			_showingError = true;
			try
			{
				var message = e.Exception.Message + Environment.NewLine + e.Exception.StackTrace;
				System.Windows.MessageBox.Show(message.Substring(0,Math.Min(message.Length,1000)),
					"Unhandled Exception", System.Windows.MessageBoxButton.OK);
				Shutdown(-1);
			}
			finally
			{
				_showingError = false;
				e.Handled = true;
			}
		}

	}
}
