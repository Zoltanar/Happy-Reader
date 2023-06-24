using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Happy_Apps_Core.Translation;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View.Tabs
{
	public partial class TestTranslationTab : UserControl
	{
		private TranslationTester _viewModel;
		private readonly ToolTip _mouseoverTip;

		public TestTranslationTab()
		{
			InitializeComponent();
			_mouseoverTip = StaticMethods.CreateMouseoverTooltip(OriginalTextBox, PlacementMode.Bottom);
		}

		[UsedImplicitly]
		private bool EntryGameFilter(string input, object item)
		{
			//Short input is not filtered to prevent excessive loading times
			if (input.Length <= 4) return false;
			var gameData = (EntryGame)item;
			var result = gameData.ToString().ToLowerInvariant().Contains(input.ToLowerInvariant());
			return result;
		}

		private void SetEntryGameEnter(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			UpdateEntryGame(sender);
		}

		private void SetEntryGameLeftClick(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Released) return;
			UpdateEntryGame(sender);
		}

		private void UpdateEntryGame(object sender)
		{
			var acb = (AutoCompleteBox)sender;
			var binding = acb.GetBindingExpression(AutoCompleteBox.TextProperty);
			Debug.Assert(binding != null, nameof(binding) + " != null");
			binding.UpdateSource();
		}

		private void TestTranslationClick(object sender, RoutedEventArgs e) => _viewModel.Test();

		private void TestTranslationPanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			_viewModel = (TranslationTester)DataContext;
		}

        private void ClickDeleteButton(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (DisplayEntry)button.DataContext;
            if (item.Id == 0) return;
            if (item.DeletePrimed) _viewModel.DeleteEntry(item);
            else item.PrimeDeletion(button);
        }

        private void OnMouseover(object sender, MouseEventArgs e)
		{
			if (!StaticMethods.MainWindow.ViewModel.SettingsViewModel.TranslatorSettings.MouseoverDictionary) return;
			var mousePoint = Mouse.GetPosition(OriginalTextBox);
			var textPosition = OriginalTextBox.GetCharacterIndexFromPoint(mousePoint, false);
			if (textPosition == -1) return;
			var text = OriginalTextBox.Text.Substring(textPosition);
			StaticMethods.UpdateTooltip(_mouseoverTip, text);
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			if (_mouseoverTip?.IsOpen ?? false) _mouseoverTip.IsOpen = false;
		}

        private void DataGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.Row.DataContext is not CachedTranslation { Source: CachedTranslation.UserEnteredSource} translation) return;
            StaticMethods.Data.Translations.Upsert(translation, true);
        }
    }
}
