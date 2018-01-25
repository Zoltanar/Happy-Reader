using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for VNPanel.xaml
    /// </summary>
    public partial class VNPanel : UserControl
    {
        private MainWindow _mainWindow;
        private readonly ListedVN _viewModel;
        public VNPanel(ListedVN vn)
        {
            InitializeComponent();
            _viewModel = vn;
            DataContext = vn;
            var cvnItems = StaticHelpers.LocalDatabase.CharacterVNs.Where(cvn => cvn.ListedVNId == vn.VNID).ToArray();
            CharacterTiles.ItemsSource = cvnItems.Select(CharacterTile.FromCharacterVN);
            if (vn.VNID != 0 && vn.DbTags.Count > 0)
            {
                var groups = vn.DbTags.GroupBy(x => x.Category);
                foreach (var group in groups)
                {
                    if (group.Key == null) continue;
                    var inlines = new List<Inline>();
                    inlines.Add(new Run($"{group.Key}: "));
                    foreach (var tag in group.OrderByDescending(x => x.Score))
                    {
                        var link = new Hyperlink(new Run(tag.Print())) { Tag = tag };
                        link.PreviewMouseLeftButtonDown += OnTagClick;
                        inlines.Add(link);
                    }
                    switch (group.Key)
                    {
                        case StaticHelpers.TagCategory.Null:
                            continue;
                        case StaticHelpers.TagCategory.Technical:
                            TechnicalTagsControl.ItemsSource = inlines;
                            continue;
                        case StaticHelpers.TagCategory.Sexual:
                            SexualTagsControl.ItemsSource = inlines;
                            continue;
                        case StaticHelpers.TagCategory.Content:
                            ContentTagsControl.ItemsSource = inlines;
                            continue;
                    }
                }
            }

        }

        private async void OnTagClick(object sender, MouseButtonEventArgs e)
        {
            _mainWindow.MainTabControl.SelectedIndex = 3;
            var tag = (DbTag)((Hyperlink)sender).Tag;
            await ((MainWindowViewModel)_mainWindow.DataContext).DatabaseViewModel.ShowTagged(DumpFiles.PlainTags.Find(item => item.ID == tag.TagId));
        }

        private async void VNPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            _mainWindow = (MainWindow)Window.GetWindow(this);
            await _viewModel.GetRelationsAnimeScreens();
            ScreensBox.AspectRatio = _viewModel.ScreensObject.Any() ? _viewModel.ScreensObject.Max(x => (double)x.Width / x.Height) : 1;
            ImageBox.MaxHeight = ImageBox.Source.Height;
            RelationsCombobox.SelectedIndex = 0;
            AnimeCombobox.SelectedIndex = 0;
        }

        private void ScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollviewer = (ScrollViewer)sender;
            scrollviewer.CanContentScroll = true;
            if (e.Delta > 0) scrollviewer.LineLeft();
            else scrollviewer.LineRight();
            e.Handled = true;
        }

        private void ID_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start($"https://vndb.org/v{_viewModel.VNID}");
        }
    }
}
