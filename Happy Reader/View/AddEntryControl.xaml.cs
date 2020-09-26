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
        private readonly Entry _entry;
        private bool _entryAlreadyAdded;

        internal AddEntryControl([NotNull]MainWindowViewModel mainViewModel, string input, string output, bool seriesSpecific)
        {
            _mainViewModel = mainViewModel;
            InitializeComponent();
            var enumTypes = Enum.GetValues(typeof(EntryType)).Cast<EntryType>();
            TypeCb.ItemsSource = enumTypes;
            _entry = new Entry(input, output)
            {
	            SeriesSpecific = seriesSpecific,
	            GameId = _mainViewModel.UserGame?.VN?.VNID,
	            UserId = _mainViewModel.User?.Id ?? 0
            };
            DataContext = _entry;
            _entry.OnPropertyChanged();
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
	        _entry.Time = DateTime.UtcNow;
            StaticMethods.Data.Entries.Add(_entry);
            StaticMethods.Data.SaveChanges();
            ResponseLabel.Content = $@"Entry was added (id {_entry.Id}).";
            _entryAlreadyAdded = true;
            _mainViewModel.EntriesViewModel.SetEntries();
	        _mainViewModel.Translator.RefreshEntries = true;
            Cancel_Click(this, null);
        }

        private bool ValidateEntry()
        {
	        _entry.Output ??= string.Empty;
	        if (string.IsNullOrWhiteSpace(_entry.RoleString))
	        {
		        switch (_entry.Type)
		        {
          case EntryType.Name:
	          _entry.RoleString = "m";
	          break;
          case EntryType.Translation:
	          _entry.RoleString = "n";
            break;
          case EntryType.Proxy:
	          ResponseLabel.Content = $@"Entries of type '{_entry.Type}' require a role.";
	          return false;
		        }
	        } 
	        if (_entryAlreadyAdded)
	        {
		        ResponseLabel.Content = @"Entry was already added.";
		        return false;
	        }
	        if (string.IsNullOrWhiteSpace(_entry.Input))
	        {
		        ResponseLabel.Content = @"Please type something in Input box.";
		        return false;
	        }
	        return true;
        }
    }
}
