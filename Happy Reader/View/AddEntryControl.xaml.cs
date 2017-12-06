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
        private bool _entryAlreadyAdded;

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
            if (!Validate()) return;
            var entry = new Entry
            {
                GameId = _mainViewModel.Game.Id,
                Input = InputTb.Text,
                Output = OutputTb.Text,
                RoleString = string.IsNullOrWhiteSpace(RoleTb.Text) ? null : RoleTb.Text,
                Type = (EntryType)TypeCb.SelectedItem,
                Private = PrivateChb.IsChecked ?? false,
                SeriesSpecific = SeriesSpecificChb.IsChecked ?? false,
                Regex = RegexChb.IsChecked ?? false,
                Disabled = !(EnabledChb.IsChecked ?? false),
                Comment = CommentTb.Text,
                UserId = _mainViewModel.User?.Id ?? 0,
                Time = DateTime.UtcNow,
                Id = StaticMethods.Data.Entries.Max(x=>x.Id)+1
            };
            StaticMethods.Data.Entries.Add(entry);
            StaticMethods.Data.SaveChanges();
            ResponseLabel.Content = $@"Entry was added (id {entry.Id}).";
            _entryAlreadyAdded = true;

            bool Validate()
            {
                if (_entryAlreadyAdded)
                {
                    ResponseLabel.Content = @"Entry was already added.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(InputTb.Text))
                {
                    ResponseLabel.Content = @"Please type something in Input box.";
                    return false;
                }
                if (_mainViewModel.Game == null)
                {
                    ResponseLabel.Content = @"There is no active game.";
                    return false;
                }
                return true;
            }
        }
    }
}
