using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Model.VnFilters;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader.ViewModel
{
	public class FiltersViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private CustomVnFilter _customFilter;
		private int _selectedFilterIndex;
		public ObservableCollection<CustomVnFilter> Filters { get; private set; }
		public CustomVnFilter PermanentFilter { get; set; }
		public CustomVnFilter CustomFilter
		{
			get => _customFilter;
			set
			{
				_customFilter = value == null ? new CustomVnFilter() : new CustomVnFilter(value);
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
		public VnFilter NewFilter { get; } = new VnFilter();
		public ComboBoxItem[] LengthTypes { get; } = StaticMethods.GetEnumValues(typeof(LengthFilterEnum));
		public ComboBoxItem[] ReleaseTypes { get; } = StaticMethods.GetEnumValues(typeof(ReleaseStatusEnum));
		public ComboBoxItem[] Labels { get; } = StaticMethods.GetEnumValues(typeof(UserVN.LabelKind));
		public ComboBoxItem[] FilterTypes { get; } = StaticMethods.GetEnumValues(typeof(VnFilterType));
		public ComboBoxItem[] GameOwnedStatus { get; } = StaticMethods.GetEnumValues(typeof(OwnedStatus));
		public string SaveFilterError { get; set; }

		public FiltersViewModel()
		{
			SelectedFilterIndex = 0;
			LoadPermanentFilter();
			LoadVnFilters();
			CustomFilter = Filters.FirstOrDefault();
			AddToCustomFilterCommand = new CommandHandler(AddToCustomFilter, true);
			AddToPermanentFilterCommand = new CommandHandler(AddToPermanentFilter, true);
			SaveCustomFilterCommand = new CommandHandler(SaveCustomFilter, true);
		}

		private void SaveCustomFilter()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(CustomFilter.Name))
				{
					SaveFilterError = "Please enter a name.";
					return;
				}
				SaveFilterError = "";
				var existingFilter = Filters.FirstOrDefault(x => x.Name == CustomFilter.Name);
				if (existingFilter != null)
				{
					var result = MessageBox.Show($"Overwrite existing filter: {existingFilter.Name}?", "Happy Reader", MessageBoxButton.OKCancel);
					if (result == MessageBoxResult.Cancel) return;
					existingFilter.Overwrite(CustomFilter);
				}
				else Filters.Add(CustomFilter);
				SaveVnFilters();
				SaveFilterError = "Filter saved.";
			}
			finally
			{
				OnPropertyChanged();
			}
		}

		public void DeleteCustomFilter()
		{
			try
			{
				var result = MessageBox.Show($"Delete existing filter: {CustomFilter.Name}?", "Happy Reader", MessageBoxButton.OKCancel);
				if (result == MessageBoxResult.Cancel) return;
				var indexOfFilter = Filters.IndexOf(CustomFilter.OriginalFilter);
				if (indexOfFilter == -1)
				{
					SaveFilterError = "Failed to find filter in collection.";
					return;
				}
				Filters.RemoveAt(indexOfFilter);
				CustomFilter = Filters.Count == 1
					? new CustomVnFilter()
					: indexOfFilter > 0 ? Filters[indexOfFilter - 1] : Filters[indexOfFilter + 1];
				SaveVnFilters();
				SaveFilterError = "Filter removed.";
			}
			finally
			{
				OnPropertyChanged(null);
			}
		}

		private void AddToPermanentFilter()
		{
			if (NewFilterOrGroup) PermanentFilter.OrFilters.Add(NewFilter.GetCopy());
			else PermanentFilter.AndFilters.Add(NewFilter.GetCopy());
			SavePermanentFilter();
		}

		private void AddToCustomFilter()
		{
			if (NewFilterOrGroup) CustomFilter.OrFilters.Add(NewFilter.GetCopy());
			else CustomFilter.AndFilters.Add(NewFilter.GetCopy());
			OnPropertyChanged(nameof(CustomFilter));
		}

		private void LoadPermanentFilter()
		{
			try
			{
				if (File.Exists(StaticMethods.PermanentFilterJson))
				{
					var text = File.ReadAllText(StaticMethods.PermanentFilterJson);
					PermanentFilter = JsonConvert.DeserializeObject<CustomVnFilter>(text);
				}
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			if (PermanentFilter != null) return;
			PermanentFilter = new CustomVnFilter();
		}

		public void SavePermanentFilter()
		{
			var text = JsonConvert.SerializeObject(PermanentFilter, Formatting.Indented);
			File.WriteAllText(StaticMethods.PermanentFilterJson, text);
			OnPropertyChanged(nameof(PermanentFilter));
		}

		private void LoadVnFilters()
		{
			try
			{
				if (File.Exists(StaticMethods.CustomFiltersJson))
				{
					var text = File.ReadAllText(StaticMethods.CustomFiltersJson);
					Filters = JsonConvert.DeserializeObject<ObservableCollection<CustomVnFilter>>(text);
				}
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			if (Filters != null) return;
			Filters = DefaultFilterCollectionBuilder.BuildDefaultFilters();
			SaveVnFilters();
		}

		private void SaveVnFilters()
		{
			var text = JsonConvert.SerializeObject(Filters, Formatting.Indented);
			File.WriteAllText(StaticMethods.CustomFiltersJson, text);
		}

		[NotifyPropertyChangedInvocator]
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
