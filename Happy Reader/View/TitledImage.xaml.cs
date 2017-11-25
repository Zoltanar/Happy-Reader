using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
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
            var localImage = usergame.VN.StoredCover;
            if (!File.Exists(localImage)) return;
            Uri imageUri = new Uri(Path.GetFullPath(localImage), UriKind.Absolute);
            BitmapImage imageBitmap = new BitmapImage(imageUri);
            Image.Source = imageBitmap;
        }

        public string FilePath => _viewModel.FilePath;

        private void GameDetails(object sender, EventArgs e)
        {
            var window = (MainWindow)Window.GetWindow(this);
            if(window == null) throw new NullReferenceException("MainWindow not found.");
            var tabItem = new TabItem {Header = _viewModel.VN.Title, Content = new UserGamePanel(_viewModel)};
            window.MainTabControl.Items.Add(tabItem);
        }
    }
}
