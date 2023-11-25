using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader.ViewModel
{
	/// <summary>
	/// Class to modify a custom filter.
	/// </summary>
	public class FiltersViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
        private CustomFilter _customFilter;
        private CustomFilter _customFilterCopy;
        private int _selectedFilterIndex;
		private readonly DatabaseViewModelBase _databaseViewModel;
		public ObservableCollection<CustomFilter> Filters { get; }
		public CustomFilter PermanentFilter { get; }
        public CustomFilter CustomFilter
        {
            get => _customFilter;
            set
            {
                _customFilter = value ?? new CustomFilter();
                CustomFilterCopy = _customFilter.GetCopy();
                OnPropertyChanged();
            }
        }
        public CustomFilter CustomFilterCopy
		{
			get => _customFilterCopy;
			set
			{
                _customFilterCopy = value;
				OnPropertyChanged();
			}
		}
		public ICommand AddToCustomFilterCommand { get; set; }
		public ICommand AddToPermanentFilterCommand { get; set; }
		public ICommand SaveCustomFilterCommand { get; set; }
		public bool NewFilterOrGroup { get; set; }

		public int SelectedFilterIndex
		{
			get => _selectedFilterIndex;
			set
			{
				_selectedFilterIndex = value;
				OnPropertyChanged();
			}
		}

		public IFilter NewFilter { get; } = new GeneralFilter();
		public ComboBoxItem[] FilterTypes { get; } = StaticMethods.GetEnumValues(typeof(GeneralFilterType));

		public string SaveFilterError { get; set; }
		
		public FiltersViewModel(ObservableCollection<CustomFilter> customFilters, CustomFilter permanentFilter, DatabaseViewModelBase databaseViewModel)
		{
			_databaseViewModel = databaseViewModel;
			Filters = customFilters;
            Filters.CollectionChanged += FiltersCollectionChanged;
			PermanentFilter = permanentFilter;
			SelectedFilterIndex = 0;
			//CustomFilter = Filters.FirstOrDefault();
			AddToCustomFilterCommand = new CommandHandler(AddToCustomFilter, true);
			AddToPermanentFilterCommand = new CommandHandler(AddToPermanentFilter, true);
			SaveCustomFilterCommand = new CommandHandler(SaveCustomFilter, true);
		}

        private void FiltersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove) return;
			SaveToFile(false);
        }

        public void SaveCustomFilter()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(CustomFilterCopy.Name))
				{
					SaveFilterError = "Please enter a name.";
					return;
				}
				SaveFilterError = "";
				var existingFilter = Filters.FirstOrDefault(x => x.Name == CustomFilterCopy.Name);
				if (existingFilter != null)
				{
					var result = MessageBox.Show($"Overwrite existing filter: {existingFilter.Name}?", "Happy Reader", MessageBoxButton.OKCancel);
					if (result == MessageBoxResult.Cancel) return;
					existingFilter.Overwrite(CustomFilterCopy);
				}
				else
				{
					Filters.Add(CustomFilterCopy);
                    CustomFilterCopy = new CustomFilter();
				}
				SaveToFile(false);
				SaveFilterError = "Filter saved.";
			}
			finally
			{
				OnPropertyChanged(null);
			}
		}

		public void DeleteCustomFilter()
		{
			try
			{
				var result = MessageBox.Show($"Delete existing filter: {CustomFilter.Name}?", "Happy Reader", MessageBoxButton.OKCancel);
				if (result == MessageBoxResult.Cancel) return;
				var indexOfFilter = Filters.IndexOf(CustomFilter);
				if (indexOfFilter == -1)
				{
					SaveFilterError = "Failed to find filter in collection.";
					return;
				}
				Filters.RemoveAt(indexOfFilter);
				CustomFilter = Filters.Count == 1
					? new CustomFilter()
					: indexOfFilter > 0 ? Filters[indexOfFilter - 1] : Filters[indexOfFilter + 1];
				SaveToFile(false);
				SaveFilterError = "Filter removed.";
			}
			finally
			{
				OnPropertyChanged(null);
			}
		}

		public void AddToPermanentFilter()
		{
			if (NewFilterOrGroup) PermanentFilter.OrFilters.Add(NewFilter.GetCopy());
			else PermanentFilter.AndFilters.Add(NewFilter.GetCopy());
			SaveToFile(true);
		}

		public void AddToCustomFilter()
		{
			OnPropertyChanged(nameof(NewFilter));
			if (NewFilterOrGroup) CustomFilterCopy.OrFilters.Add(NewFilter.GetCopy());
			else CustomFilterCopy.AndFilters.Add(NewFilter.GetCopy());
			OnPropertyChanged(nameof(CustomFilterCopy));
		}
		
		public void SaveToFile(bool isPermanent)
		{
			 var text = JsonConvert.SerializeObject(StaticMethods.AllFilters, Formatting.Indented, StaticMethods.SerialiserSettings);
			File.WriteAllText(StaticMethods.AllFiltersJson, text);
			if(isPermanent) OnPropertyChanged(nameof(PermanentFilter));
		}
		
		public void SaveOrGroup(bool isPermanent)
		{
			var filter = isPermanent ? PermanentFilter : CustomFilterCopy;
			filter.SaveOrGroup();
			if (isPermanent) SaveToFile(true);
		}

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public async Task ApplyCurrentFilter()
		{
			await _databaseViewModel.ChangeFilter(CustomFilterCopy);
		}

        public void SelectFilter(CustomFilter filter)
        {
            _databaseViewModel.SelectedFilterIndex = Filters.IndexOf(filter);
        }
    }
}