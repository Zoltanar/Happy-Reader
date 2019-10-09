using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Happy_Reader.ViewModel;
using Point = System.Windows.Point;

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
			if (!IsVisible) Show();
			_viewModel.AddTranslation(translation);
			_viewModel.UpdateOutput();
            if (FullScreenOn) Activate();
        }

        internal void SetLocation(Rectangle rectangle)
        {
	        Application.Current.Dispatcher.Invoke(() =>
	        {
		        Left = rectangle.X;
		        Top = rectangle.Y;
		        Width = rectangle.Width;
		        Height = rectangle.Height;
	        });
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

	    public Rectangle GetRectangle()
	    {
		    Rectangle result = default;
			Application.Current.Dispatcher.Invoke(()=> result = new Rectangle((int)Left, (int)Top, (int)Width, (int)Height));
		    return result;
		}

	    private bool _isResizing;
	    private Point _startPosition;

	    private void Resizer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	    {
			if (!Mouse.Capture(Resizer)) return;
			_isResizing = true;
			_startPosition = Mouse.GetPosition(this);
		}

		private void Resizer_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (!_isResizing) return;
			Point currentPosition = Mouse.GetPosition(this);
			double diffX = currentPosition.X - _startPosition.X;
			double diffY = currentPosition.Y - _startPosition.Y;
			Left += diffX;
			Top += diffY;
			Width -= diffX;
			Height -= diffY;
		}

		private void Resizer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (!_isResizing) return;
			_isResizing = false;
			Mouse.Capture(null);
		}
	}
}
