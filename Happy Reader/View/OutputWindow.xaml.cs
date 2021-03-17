using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	public partial class OutputWindow
	{
		private readonly GridLength _settingsColumnLength;
		private OutputWindowViewModel _viewModel;
		private bool _isResizing;
		private bool _settingsOn = true;
		private System.Windows.Point _startPosition;

		public bool SettingsOn
		{
			get => _settingsOn;
			set
			{
				if (_settingsOn == value) return;
				_settingsOn = value;
				UpdateSettingToggles();
			}
		}
		public bool FullScreenOn { get; set; }

		public OutputWindow()
		{
			InitializeComponent();
			_settingsColumnLength = SettingsColumn.Width;
		}
		
		private void UpdateSettingToggles()
		{
			StaticMethods.Settings.TranslatorSettings.SettingsViewState = SettingsOn;
			SettingsColumn.Width = SettingsOn ? _settingsColumnLength : new GridLength(22);
			OpacityLabel.Visibility = SettingsOn ? Visibility.Visible : Visibility.Collapsed;
			foreach (var toggleButton in SettingsPanel.Children.OfType<ContentControl>())
			{
				var value = toggleButton.Tag as string;
				if (string.IsNullOrWhiteSpace(value)) continue;
				var parts = value.Split(',');
				toggleButton.Content = parts[SettingsOn ? 1 : 0];
			}
		}

		public bool InitialisedWindowLocation { get; set; }

		public Action InitialiseWindowForGame { get; set; }

		public void AddTranslation(Translation translation)
		{
			if (!InitialisedWindowLocation) InitialiseWindowForGame?.Invoke();
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
			_viewModel.Initialize(() => OutputTextBox.Selection.Text, OutputTextBox.Document, () => Dispatcher.Invoke(()=> OutputTextBox.ScrollToEnd()));
			SettingsOn = StaticMethods.Settings.TranslatorSettings.SettingsViewState;
			var tColor = StaticMethods.Settings.TranslatorSettings.TranslatedColor.Color.Color;
			var darkerColor = System.Windows.Media.Color.FromRgb((byte)(tColor.R * 0.75), (byte)(tColor.G * 0.75), (byte)(tColor.B * 0.75));
			var dropShadowEffect = new System.Windows.Media.Effects.DropShadowEffect
			{
				Color = darkerColor
			};
			OutputTextBox.Effect = dropShadowEffect;
		}

		public Rectangle GetRectangle()
		{
			Rectangle result = default;
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() => result = new Rectangle((int)Left, (int)Top, (int)Width, (int)Height));
			return result;
		}

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

		private void CopyOriginalClick(object sender, RoutedEventArgs e)
		{
			var curBlock = OutputTextBox.Document.Blocks.FirstOrDefault(x =>
				x.ContentStart.CompareTo(OutputTextBox.CaretPosition) == -1 &&
				x.ContentEnd.CompareTo(OutputTextBox.CaretPosition) == 1);
			if (curBlock?.Tag is Translation translation) Clipboard.SetText(translation.Original);
		}

		private void HorizontalAlignmentClick(object sender, RoutedEventArgs e)
		{
			StaticMethods.Settings.TranslatorSettings.SetNextHorizontalAlignmentState();
			_viewModel.UpdateOutput();
		}

		private void VerticalAlignmentClick(object sender, RoutedEventArgs e)
		{
			var newState = StaticMethods.Settings.TranslatorSettings.SetNextVerticalAlignmentState();
			_viewModel.UpdateOutput();
			if (newState == VerticalAlignment.Top) OutputTextBox.ScrollToHome();
		}
	}
}
