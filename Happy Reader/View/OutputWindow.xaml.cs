using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	public partial class OutputWindow
	{
		private readonly Action _initialiseWindowForGame;
		private readonly GridLength _settingsColumnLength;
		private OutputWindowViewModel _viewModel;
		private bool _loaded;
		private bool _isResizing;
		private bool _settingsOn = true;
		private Point _startPosition;

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

		public OutputWindow(Action initialiseOutputWindowForGame)
		{
			InitializeComponent();
			_initialiseWindowForGame = initialiseOutputWindowForGame;
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

		public void AddTranslation(Translation translation)
		{
			if (!InitialisedWindowLocation) _initialiseWindowForGame?.Invoke();
			if (!IsVisible && !translation.IsError) Show();
			if (!_loaded) OutputWindow_OnLoaded(null, null);
			_viewModel.AddTranslation(translation);
			_viewModel.UpdateOutput();
			if (FullScreenOn) Activate();
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
			_viewModel = (OutputWindowViewModel)DataContext;
			_viewModel.Initialize(() => OutputTextBox.Selection.Text, OutputTextBox.Document, () => Dispatcher.Invoke(OutputTextBox.ScrollToEnd));
			SettingsOn = StaticMethods.Settings.TranslatorSettings.SettingsViewState;
			var tColor = StaticMethods.Settings.TranslatorSettings.TranslatedColor.Color.Color;
			var darkerColor = System.Windows.Media.Color.FromRgb((byte)(tColor.R * 0.75), (byte)(tColor.G * 0.75), (byte)(tColor.B * 0.75));
			var dropShadowEffect = new System.Windows.Media.Effects.DropShadowEffect
			{
				Color = darkerColor
			};
			OutputTextBox.Effect = dropShadowEffect;
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
			_viewModel.UpdateOutput();
		}

		private void VerticalAlignmentClick(object sender, RoutedEventArgs e)
		{
			var newState = StaticMethods.Settings.TranslatorSettings.SetNextVerticalAlignmentState();
			_viewModel.UpdateOutput();
			if (newState == VerticalAlignment.Top) OutputTextBox.ScrollToHome();
		}

		private void SizeOrLocationChanged(object sender, EventArgs e)
		{
			if (!InitialisedWindowLocation || StaticMethods.MainWindow.ViewModel.UserGame == null) return;
			var outputWindowLocation = new NativeMethods.RECT
			{
				Left = (int)Left,
				Top = (int)Top,
				Right = (int)Left + (int)Width,
				Bottom = (int)Top + (int)Height
			};
			StaticMethods.MainWindow.ViewModel.UserGame.GameHookSettings.SaveOutputRectangle(outputWindowLocation);
		}

		private void OnMouseover(object sender, MouseEventArgs e)
		{
			if (!StaticMethods.MainWindow.ViewModel.SettingsViewModel.TranslatorSettings.MouseoverDictionary) return;
			var mousePoint = Mouse.GetPosition(OutputTextBox);
			var textPosition = OutputTextBox.GetPositionFromPoint(mousePoint, false);
			if (textPosition != null)
			{
				if (textPosition.Paragraph?.Tag is not Translation trans || trans.OriginalBlock != textPosition.Paragraph) return;
				var text = textPosition.GetTextInRun(LogicalDirection.Forward);
				var results = StaticMethods.MainWindow.ViewModel.Translator.OfflineDictionary.Search(text);
				if (results.Count < 1) return;
				text = string.Join(Environment.NewLine, results.Select(c => c.Detail()).Take(5));
				if (text.Equals(MouseoverTip.Content)) return;
				MouseoverTip.Content = text;
				if(!MouseoverTip.IsOpen) MouseoverTip.IsOpen = true;
			}
		}

		private void OpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			MouseoverTipBackground.Opacity = e.NewValue;
		}
	}
}
