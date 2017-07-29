using System;
using System.Linq;
using System.Windows.Controls;
using Happy_Reader.Database;

namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for AddEntryControl.xaml
    /// </summary>
    public partial class AddEntryControl
    {
        private readonly MainWindowViewModel _mainViewModel;
        internal AddEntryControl(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            InitializeComponent();
            GameTb.Text = _mainViewModel.Game.RomajiTitle ?? _mainViewModel.Game.Title;
            var enumTypes = Enum.GetValues(typeof(EntryType))
                .Cast<EntryType>();
            TypeCb.ItemsSource = enumTypes;
        }

        private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var tabItem = Parent as TabItem;
            (tabItem?.Parent as TabControl)?.Items.Remove(tabItem);
        }

        private void AddEntry_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //TODO validate
            var entry = new Entry
            {
                GameId = _mainViewModel.Game.Id,
                Input = InputTb.Text,
                Output = OutputTb.Text,
                RoleString = RoleTb.Text,
                Type = (EntryType)TypeCb.SelectedItem,
                Private = PrivateChb.IsChecked ?? false,
                SeriesSpecific = SeriesSpecificChb.IsChecked ?? false,
                Regex = RegexChb.IsChecked ?? false,
                Disabled = !(EnabledChb.IsChecked ?? false),
                Comment = CommentTb.Text
            };
            _mainViewModel.Data.Entries.Add(entry);

        }
    }
}
