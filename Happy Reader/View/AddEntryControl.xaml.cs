using System;
using System.Linq;
using System.Windows.Controls;
using Happy_Reader.Database;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View
{
    /// <summary>
    /// Interaction logic for AddEntryControl.xaml
    /// </summary>
    public partial class AddEntryControl
    {

        private readonly MainWindowViewModel _mainViewModel;
        private bool _entryAlreadyAdded;

        internal AddEntryControl([NotNull]MainWindowViewModel mainViewModel, string initialInput = "", string initialOutput = "")
        {
            _mainViewModel = mainViewModel;
            InitializeComponent();
            GameTb.Text = _mainViewModel.UserGame?.VN?.Title ?? "None";
            var enumTypes = Enum.GetValues(typeof(EntryType))
                .Cast<EntryType>();
            TypeCb.ItemsSource = enumTypes;
            TypeCb.SelectedValue = EntryType.Name;
	        InputTb.Text = initialInput;
	        OutputTb.Text = initialOutput;

		}

        private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var tabItem = (TabItem)Parent;
            var tabControl = (TabControl)tabItem.Parent;
            tabControl.SelectedIndex = 1;
            tabControl.Items.Remove(tabItem);
        }

        private void AddEntry_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!ValidateEntry()) return;
            var entry = new Entry
            {
                GameId = _mainViewModel.UserGame?.VN?.VNID,
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
                Id = StaticMethods.Data.Entries.Max(x => x.Id) + 1
            };
            StaticMethods.Data.Entries.Add(entry);
            StaticMethods.Data.SaveChanges();
            ResponseLabel.Content = $@"Entry was added (id {entry.Id}).";
            _entryAlreadyAdded = true;
            _mainViewModel.SetEntries();
	        _mainViewModel.Translator.RefreshEntries = true;
            Cancel_Click(this, null);
        }

        private bool ValidateEntry()
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
            if (TypeCb.SelectedItem == null)
            {
                ResponseLabel.Content = @"Please select type.";
                return false;
            }
            return true;
        }
    }
}
