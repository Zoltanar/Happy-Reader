using Happy_Reader.View.Tabs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Happy_Reader.View;

namespace Happy_Reader
{
	public partial class App
	{
		private bool _showingError;
		public App()
		{
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (_showingError)
			{
				e.Handled = true;
				return;
			}
			_showingError = true;
			try
			{
				var message = $"Press Yes to continue or No to Exit.{Environment.NewLine}{e.Exception}";
				var response = MessageBox.Show(message.Substring(0,Math.Min(message.Length,1000)),
					"Unhandled Exception", MessageBoxButton.YesNo);
				if(response == MessageBoxResult.No) Shutdown(-1);
			}
			finally
			{
				_showingError = false;
				e.Handled = true;
			}
		}

        private void TabMiddleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return;
            var vnTabItem = ((DependencyObject)sender).FindParent<VNTab>();
			//we don't close tabs if they are in VnTab.
            if (vnTabItem != null) return; 
            var tabItem = ((DependencyObject)sender).FindParent<TabItem>();
            tabItem.Template = null;
            var content = tabItem.Content;
            switch (content)
            {
                case VNTab vnTab:
                    ((MainWindow)MainWindow)!.SavedData.Tabs.RemoveWhere(st => st.TypeName == nameof(VNTab) && st.Id == vnTab.ViewModel.VNID);
                    break;
                case UserGameTab gameTab:
                    ((MainWindow)MainWindow)!.SavedData.Tabs.RemoveWhere(st => st.TypeName == nameof(UserGameTab) && st.Id == gameTab.ViewModel.Id);
                    break;
                case ProducerTab producerTab:
                    ((MainWindow)MainWindow)!.SavedData.Tabs.RemoveWhere(st => st.TypeName == nameof(ProducerTab) && st.Id == producerTab.ViewModel.ID);
                    break;
                default:
                    //debug break
                    break;
            }
            ((MainWindow)MainWindow)!.MainTabControl.Items.Remove(tabItem);
            ((MainWindow)MainWindow)!.ToggleCloseTabsButton(null);
        }
    }
}
