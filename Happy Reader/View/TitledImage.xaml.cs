using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for TitledImage.xaml
    /// </summary>
    public partial class TitledImage
    {
        private readonly UserGame _viewModel;

        public TitledImage()
        {
            InitializeComponent();
        }

        public TitledImage(UserGame usergame)
        {
            InitializeComponent();
            DataContext = usergame;
            _viewModel = usergame;
            //Title.Text = StaticHelpers.TruncateString(usergame.VN.Title, 30);
            Image.Source = usergame.Image;
        }
        
        private void GameDetails(object sender, EventArgs e)
        {
            var window = (MainWindow)Window.GetWindow(this);
            if(window == null) throw new NullReferenceException("MainWindow not found.");
            var tabItem = new TabItem {Header = _viewModel.DisplayName, Content = new UserGamePanel(_viewModel, this)};
            window.MainTabControl.Items.Add(tabItem);
        }

        public void RefreshContext()
        {
            _viewModel.OnPropertyChanged(null);
        }

        private void BrowseToLocation(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", Directory.GetParent(_viewModel.FilePath).FullName);
        }
    }
}
