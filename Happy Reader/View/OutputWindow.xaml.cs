using System;
using System.Windows;
using System.Windows.Input;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow
    {
        private readonly GridLength _settingsColumnLength;
        private readonly MainWindow _mainWindow;
	    private OutputWindowViewModel _viewModel;

	    public bool SettingsOn { get; set; } = true;
	    public bool FullScreenOn { get; set; }
	    public bool LockOn { get; set; } = true;

		public OutputWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _settingsColumnLength = SettingsColumn.Width;
	        DebugTextbox.Effect = new System.Windows.Media.Effects.DropShadowEffect();
	        _viewModel = (OutputWindowViewModel) DataContext;
        }



		public void AddTranslation(Translation translation)
	    {
		    _viewModel.AddTranslation(translation);
			_viewModel.UpdateOutput();
            if (FullScreenOn) Activate();
        }

        internal void SetLocation(int left, int bottom, int width)
        {
            if (LockOn) return;
            Left = left + 0.25 * width;
            Top = bottom - Height;
            Width = width * 0.5;
        }

        private void DragOnMouseButton(object sender, MouseButtonEventArgs e)
        {
            try { DragMove(); }
            catch (InvalidOperationException) { }
        }
		
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
		
        private void OutputWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (OutputWindowViewModel)DataContext;
            _viewModel.Initialize(_mainWindow, DebugTextbox);
        }

        private void ShowSettingsOnMouseHover(object sender, MouseEventArgs e)
        {
            if (SettingsOn) return;
            // ReSharper disable once PossibleInvalidOperationException
            SettingsColumn.Width = _settingsColumnLength;
            SettingsButton.Visibility = Visibility.Hidden;
        }

        private void HideSettingsOnMouseLeave(object sender, MouseEventArgs e)
        {
            if (SettingsOn) return;
            // ReSharper disable once PossibleInvalidOperationException
            SettingsColumn.Width = new GridLength(0);
            SettingsButton.Visibility = Visibility.Visible;
        }
    }
}
