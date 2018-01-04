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
        private readonly MainWindow _mainWindow;

        public UserGameTile()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Window.GetWindow(this);
        }

        public UserGameTile(UserGame usergame)
        {
            InitializeComponent();
            DataContext = usergame;
            _viewModel = usergame;
        }

        private void GameDetails(object sender, EventArgs e)
        {
            var tabItem = new TabItem { Header = _viewModel.DisplayName, Content = new UserGamePanel(_viewModel) };
            _mainWindow.AddTabItem(tabItem);
        }

        private void BrowseToLocation(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", Directory.GetParent(_viewModel.FilePath).FullName);
        }
    }
}
