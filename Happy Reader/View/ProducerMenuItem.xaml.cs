using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for ProducerMenuItem.xaml
    /// </summary>
    public partial class ProducerMenuItem : ItemsControl
    {
        private ListedProducer Producer => (ListedProducer)DataContext;
        public ProducerMenuItem(ListedProducer producer)
        {
            InitializeComponent();
            DataContext = producer;
            SetChangeIsFavoriteHeader();
        }


        public void TransferItems(ItemsControl parent)
        {
            foreach (var item in Items.Cast<FrameworkElement>().ToList())
            {
                Items.Remove(item);
                parent.Items.Add(item);
            }
        }

        private void SetChangeIsFavoriteHeader()
        {
            ChangeIsFavoriteMenuItem.Header = Producer.IsFavorited ? "Remove from Favorites" : "Add to Favorites";
        }

        private void ShowTitlesByProducer(object sender, RoutedEventArgs e)
        {
            StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
            StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowForProducer(Producer);
        }

        private void ChangeIsFavoriteItem(object sender, RoutedEventArgs e)
        {
            if (Producer.IsFavorited)
            {
                var userListedProducer = StaticHelpers.LocalDatabase.UserProducers[(Producer.ID, StaticHelpers.LocalDatabase.CurrentUser.Id)];
                StaticHelpers.LocalDatabase.UserProducers.Remove(userListedProducer, true);
            }
            else
            {
                StaticHelpers.LocalDatabase.UserProducers.Add(new UserListedProducer
                {
                    ListedProducer_Id = Producer.ID,
                    User_Id = StaticHelpers.LocalDatabase.CurrentUser.Id
                }, true, true);
            }
            Producer.SetFavoriteProducerData(StaticHelpers.LocalDatabase, true);
            SetChangeIsFavoriteHeader();
            Producer.OnPropertyChanged(nameof(Producer.IsFavorited));
        }
    }
}
