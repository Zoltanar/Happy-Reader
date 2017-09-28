using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Happy_Apps_Core;
using Happy_Reader.Database;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for TitledImage.xaml
    /// </summary>
    public partial class TitledImage
    {
        private readonly UserGame _viewModel;
        public string FilePath;
        public TitledImage(string title, string imageSource)
        {
            InitializeComponent();
            Title.Text = title;
            if (!string.IsNullOrWhiteSpace(imageSource))
            {
                Uri imageUri = new Uri(imageSource, UriKind.Relative);
                BitmapImage imageBitmap = new BitmapImage(imageUri);
                Image.Source = imageBitmap;
            }
        }

        public TitledImage()
        {
            InitializeComponent();
        }

        public TitledImage(string filePath, UserGame usergame)
        {
            InitializeComponent();
            DataContext = usergame;
            _viewModel = usergame;
            FilePath = filePath;
            Title.Text = StaticHelpers.TruncateString(usergame.VN.Title, 30);
            var localImage = usergame.VN.StoredCover;
            if (!File.Exists(localImage)) return;
            Uri imageUri = new Uri(localImage, UriKind.RelativeOrAbsolute);
            BitmapImage imageBitmap = new BitmapImage(imageUri);
            Image.Source = imageBitmap;
        }

        private void GameDetails(object sender, EventArgs e)
        {
            var window = (MainWindow)Window.GetWindow(this);
            if(window == null) throw new NullReferenceException("MainWindow not found.");
            var tabItem = new TabItem {Header = _viewModel.VN.Title, Content = new UserGamePanel(_viewModel)};
            window.MainTabControl.Items.Add(tabItem);
        }
    }
}
