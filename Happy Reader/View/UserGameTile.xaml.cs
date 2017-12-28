using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for TitledImage.xaml
    /// </summary>
    public partial class UserGameTile
    {
        private readonly UserGame _viewModel;

        public UserGameTile()
        {
            InitializeComponent();
        }

        public UserGameTile(UserGame usergame)
        {
            InitializeComponent();
            DataContext = usergame;
            _viewModel = usergame;
        }

        private void GameDetails(object sender, EventArgs e)
        {
            var window = (MainWindow)Window.GetWindow(this);
            if (window == null) throw new NullReferenceException("MainWindow not found.");
            var tabItem = new TabItem { Header = _viewModel.DisplayName, Content = new UserGamePanel(_viewModel) };
            tabItem.MouseUp += window.TabMiddleClick;
            window.MainTabControl.Items.Add(tabItem);
            window.MainTabControl.SelectedItem = tabItem;
        }

        private void BrowseToLocation(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", Directory.GetParent(_viewModel.FilePath).FullName);
        }
    }
}
