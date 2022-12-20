using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	public partial class OutputWindow
	{
		private readonly Action<int> _initialiseWindowForGame;
		private readonly GridLength _settingsColumnLength;
		private readonly ToolTip _mouseoverTip;
		public OutputWindowViewModel ViewModel { get; private set; }
		private bool _loaded;
		private bool _isResizing;
		private Point _startPosition;
		private UserGame _userGame;

        public bool InitialisedWindowLocation { get; set; }
        public bool FullScreenOn { get; set; }

        public OutputWindow(Action<int> initialiseOutputWindowForGame, UserGame userGame)
		{
			InitializeComponent();
			_initialiseWindowForGame = initialiseOutputWindowForGame;
			_userGame = userGame;
            _settingsColumnLength = SettingsColumn.Width;
			_mouseoverTip = StaticMethods.CreateMouseoverTooltip(this, StaticMethods.Settings.TranslatorSettings.MouseoverTooltipPlacement);
			_mouseoverTip.Background.Opacity = OpacitySlider.Value;
		}

		private void UpdateSettingToggles(object sender, RoutedEventArgs e)
		{
			if (ViewModel == null) return;
			SettingsColumn.Width = ViewModel.SettingsOn ? _settingsColumnLength : new GridLength(22);
			OpacityLabel.Visibility = ViewModel.SettingsOn ? Visibility.Visible : Visibility.Collapsed;
			foreach (var toggleButton in SettingsPanel.Children.OfType<ContentControl>())
			{
				var value = toggleButton.Tag as string;
				if (string.IsNullOrWhiteSpace(value)) continue;
				var parts = value.Split(',');
				toggleButton.Content = parts[ViewModel.SettingsOn ? 1 : 0];
			}
		}


        public void AddTranslation(Translation translation, int processId)
        {
            if (!InitialisedWindowLocation) _initialiseWindowForGame?.Invoke(processId);
            if (!_loaded) OutputWindow_OnLoaded(null, null);
			var anyEnabled = ViewModel.OriginalOn || ViewModel.RomajiOn || ViewModel.TranslationOn;
			if (!IsVisible && !translation.IsError && anyEnabled) Show();
			ViewModel.AddTranslation(translation);
			if(anyEnabled) ViewModel.UpdateOutput();
			if (anyEnabled && FullScreenOn) Activate();
		}

		internal void SetLocation(NativeMethods.RECT rectangle)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				Left = rectangle.Left;
				Top = rectangle.Top;
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
			if (_loaded) return;
			ViewModel = (OutputWindowViewModel)DataContext;
			ViewModel.Initialize(() => OutputTextBox.Selection.Text, OutputTextBox.Document, () => Dispatcher.Invoke(OutputTextBox.ScrollToEnd));
			ViewModel.SettingsOn = StaticMethods.Settings.TranslatorSettings.SettingsViewState;
			ViewModel.OriginalOn = StaticMethods.Settings.TranslatorSettings.OutputOriginal;
			ViewModel.RomajiOn = StaticMethods.Settings.TranslatorSettings.OutputRomaji;
            ViewModel.TranslationOn = StaticMethods.Settings.TranslatorSettings.OutputTranslation;
            var tColor = StaticMethods.Settings.TranslatorSettings.TranslatedColor.Color.Color;
			var darkerColor = System.Windows.Media.Color.FromRgb((byte)(tColor.R * 0.75), (byte)(tColor.G * 0.75), (byte)(tColor.B * 0.75));
			var dropShadowEffect = new System.Windows.Media.Effects.DropShadowEffect
			{
				Color = darkerColor
			};
			OutputTextBox.Effect = dropShadowEffect;
			UpdateSettingToggles(sender, e);
			ViewModel.OnPropertyChanged(null);
			_loaded = true;
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
			ViewModel.UpdateOutput();
		}

		private void VerticalAlignmentClick(object sender, RoutedEventArgs e)
		{
			var newState = StaticMethods.Settings.TranslatorSettings.SetNextVerticalAlignmentState();
			ViewModel.UpdateOutput();
			if (newState == VerticalAlignment.Top) OutputTextBox.ScrollToHome();
		}

		private void SizeOrLocationChanged(object sender, EventArgs e)
		{
			if (!InitialisedWindowLocation) return;
			var outputWindowLocation = new NativeMethods.RECT
			{
				Left = (int)Left,
				Top = (int)Top,
				Right = (int)Left + (int)Width,
				Bottom = (int)Top + (int)Height
			};
            _userGame.GameHookSettings.SaveOutputRectangle(outputWindowLocation);
		}

		private void OnMouseover(object sender, MouseEventArgs e)
		{
			if (!StaticMethods.MainWindow.ViewModel.SettingsViewModel.TranslatorSettings.MouseoverDictionary) return;
			var mousePoint = Mouse.GetPosition(OutputTextBox);
			var textPosition = OutputTextBox.GetPositionFromPoint(mousePoint, false);
			if (textPosition?.Paragraph?.Tag is not Translation trans || trans.OriginalBlock != textPosition.Paragraph) return;
			var text = textPosition.GetTextInRun(LogicalDirection.Forward);
			StaticMethods.UpdateTooltip(_mouseoverTip, text);
		}

		private void OpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (_mouseoverTip == null) return;
			_mouseoverTip.Background.Opacity = e.NewValue;
		}

		private void OutputWindow_OnMouseLeave(object sender, MouseEventArgs e)
		{
			if (_mouseoverTip?.IsOpen ?? false) _mouseoverTip.IsOpen = false;
		}
	}
}
