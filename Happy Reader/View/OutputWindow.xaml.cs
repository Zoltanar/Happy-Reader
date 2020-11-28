using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	public partial class OutputWindow
	{
		private readonly GridLength _settingsColumnLength;
		private OutputWindowViewModel _viewModel;

		public bool SettingsOn { get; set; }
		public bool FullScreenOn { get; set; }

		public OutputWindow()
		{
			InitializeComponent();
			_settingsColumnLength = SettingsColumn.Width;
			var tColor = ((SolidColorBrush) StaticMethods.TranslatorSettings.TranslationColor).Color;
			var darkerColor = System.Windows.Media.Color.FromRgb((byte) (tColor.R * 0.75), (byte) (tColor.G * 0.75), (byte) (tColor.B * 0.75));
			var dropShadowEffect = new System.Windows.Media.Effects.DropShadowEffect
			{
				Color = darkerColor
			};
			OutputTextBox.Effect = dropShadowEffect;
			_viewModel = (OutputWindowViewModel)DataContext;
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
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
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
			_viewModel.Initialize(() =>OutputTextBox.Selection.Text, OutputTextBox.Document);
		}

		private void ShowSettingsOnMouseHover(object sender, MouseEventArgs e)
		{
			if (SettingsOn) return;
			SettingsColumn.Width = _settingsColumnLength;
			SettingsButton.Visibility = Visibility.Hidden;
		}

		private void HideSettingsOnMouseLeave(object sender, MouseEventArgs e)
		{
			if (SettingsOn) return;
			SettingsColumn.Width = new GridLength(0);
			SettingsButton.Visibility = Visibility.Visible;
		}

		public Rectangle GetRectangle()
		{
			Rectangle result = default;
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() => result = new Rectangle((int)Left, (int)Top, (int)Width, (int)Height));
			return result;
		}

		private bool _isResizing;
		private System.Windows.Point _startPosition;

		private void Resizer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!Mouse.Capture(Resizer)) return;
			_isResizing = true;
			_startPosition = Mouse.GetPosition(this);
		}

		private void Resizer_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (!_isResizing) return;
			var currentPosition = Mouse.GetPosition(this);
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

		public void MoveByDifference(NativeMethods.RECT rect)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				Left += rect.Left;
				Top += rect.Top;
				Width = Math.Max(100, Width + rect.Width);
				Height = Math.Max(30, Height + rect.Height);
			});
		}
	}
}
