﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
    /// <summary>
    /// Interaction logic for VNTab.xaml
    /// </summary>
    public partial class DatabaseTab : UserControl
    {
        private VNTabViewModel _viewModel;
        private MainWindow _mainWindow;

        public DatabaseTab() => InitializeComponent();

        private async void UpdateURT(object sender, RoutedEventArgs e) => await _viewModel.UpdateURT();

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!(e.VerticalChange > 0)) return;
            var loc = e.VerticalOffset + e.ViewportHeight * 2;
	        if (loc < e.ExtentHeight) return;
	        _viewModel.AddListedVNPage();
	        ((ScrollViewer)e.OriginalSource).ScrollToVerticalOffset(loc);
        }

        private async void ShowAll(object sender, RoutedEventArgs e) => await _viewModel.RefreshListedVns(true);

        private async void SearchForVN(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            await _viewModel.SearchForVN(((TextBox)sender).Text);
        }

        private void VNTab_OnLoaded(object sender, RoutedEventArgs e)
        {
	        if (DesignerProperties.GetIsInDesignMode(this)) return;
            _viewModel = (VNTabViewModel)DataContext;
            _mainWindow = (MainWindow)Window.GetWindow(this);
        }

        private void VNTileDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            var item = VisualNovelItems.SelectedItem as Tiles.VNTile;
            var vn = (ListedVN)item?.DataContext;
            if (vn == null) return;
            _mainWindow.OpenVNPanel(vn);
        }

        private async void ShowURT(object sender, RoutedEventArgs e) => await _viewModel.ShowURT();

        private async void UpdateForYear(object sender, RoutedEventArgs e)
        {
            if (!DetermineYears(out int fromYear, out int _)) return;
            await _viewModel.UpdateForYear(fromYear);
        }

        private async void UpdateCharactersForYear(object sender, RoutedEventArgs e)
        {
            if (!DetermineYears(out int fromYear, out int _)) return;
            await _viewModel.UpdateCharactersForYear(fromYear);
        }

        private async void FetchForYear(object sender, RoutedEventArgs e)
        {
            if (!DetermineYears(out int fromYear, out int toYear)) return;
            await _viewModel.FetchForYear(fromYear, toYear);
        }

        private bool DetermineYears(out int fromYear, out int toYear)
        {
            fromYear = 0;
            toYear = VndbConnection.VndbAPIMaxYear;
            string text = SearchTexBox.Text;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (text.StartsWith(">") && text.Length > 1)
            {
                text = text.Substring(1);
                if (!int.TryParse(text, out fromYear)) return false; //show error
            }
            else if (text.StartsWith("<") && text.Length > 1)
            {
                text = text.Substring(1);
                if (!int.TryParse(text, out toYear)) return false; //show error
            }
            else if (text.Contains("-") && text.Length > 2)
            {
                int index = text.IndexOf("-", StringComparison.Ordinal);
                if (!int.TryParse(text.Substring(0, index), out fromYear)) return false; //show error
                if (!int.TryParse(text.Substring(index + 1), out toYear)) return false; //show error
            }
            else
            {
                if (!int.TryParse(text, out fromYear)) return false; //show error
                toYear = fromYear;
            }
            return true;
        }
		
		private async void ToggleFiltersJapanese(object sender, RoutedEventArgs e)
		{
			_viewModel.ToggleFiltersJapanese();
			if (FiltersCb.SelectedItem != null) await _viewModel.ChangeFilter(FiltersCb.SelectedItem as CustomVnFilter);
		}
	}
}