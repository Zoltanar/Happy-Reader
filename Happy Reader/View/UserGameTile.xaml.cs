using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;

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
            var tabItem = new TabItem { Header = _viewModel.DisplayName, Content = new UserGamePanel(_viewModel) };
            // ReSharper disable once PossibleNullReferenceException
            ((MainWindow)Window.GetWindow(this)).AddTabItem(tabItem);
        }

        private void BrowseToLocation(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", Directory.GetParent(_viewModel.FilePath).FullName);
        }

        private void LaunchWithIth(object sender, RoutedEventArgs e)
        {
            // ReSharper disable once PossibleNullReferenceException
            ((MainWindowViewModel)((MainWindow) Window.GetWindow(this)).DataContext).HookWithIth(_viewModel);
        }
    }
}
