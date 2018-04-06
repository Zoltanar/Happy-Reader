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
		public CustomVnFilter PermanentFilter { get; set; } = new CustomVnFilter();
		public CustomVnFilter CustomFilter
		{
			get => _customFilter;
			set
			{
				_customFilter = new CustomVnFilter(value);
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
		public ComboBoxItem[] LengthTypes { get; } = GetEnumValues(typeof(LengthFilterEnum));
		public ComboBoxItem[] ReleaseTypes { get; } = GetEnumValues(typeof(ReleaseStatusEnum));
		public ComboBoxItem[] WishlistTypes { get; } = GetEnumValues(typeof(WishlistStatus));
		public ComboBoxItem[] UserlistTypes { get; } = GetEnumValues(typeof(UserlistStatus));
		public ComboBoxItem[] FilterTypes { get; } = GetEnumValues(typeof(VnFilterType));
		public string SaveFilterError { get; set; }

		public ObservableCollection<VnFilter> CustomFilterAndFilters => CustomFilter.AndFilters;
		public ObservableCollection<VnFilter> CustomFilterOrFilters => CustomFilter.OrFilters;


		public FiltersViewModel()
		{
			SelectedFilterIndex = 0;
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
				if (String.IsNullOrWhiteSpace(CustomFilter.Name))
				{
					SaveFilterError = "Please enter a name.";
					return; //todo error please enter a name
				}
				SaveFilterError = "";
				var existingFilter = Filters.FirstOrDefault(x => x.Name == CustomFilter.Name);
				if (existingFilter != null)
				{
					//todo ask user overwrite?
					var result = MessageBox.Show("Overwrite existing filter?", "Happy Reader", MessageBoxButton.OKCancel);
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


		public static ComboBoxItem[] GetEnumValues(Type enumType)
		{
			var result = Enum.GetValues(enumType);
			if (result.Length == 0) return new ComboBoxItem[0];
			return result.Cast<Enum>().Select(x => new ComboBoxItem { Content = x.GetDescription(), Tag = x }).ToArray();
		}

		private void AddToPermanentFilter()
		{
			if (NewFilterOrGroup) PermanentFilter.OrFilters.Add(NewFilter.GetCopy());
			else PermanentFilter.AndFilters.Add(NewFilter.GetCopy());
			OnPropertyChanged(nameof(PermanentFilter));
		}

		private void AddToCustomFilter()
		{
			if (NewFilterOrGroup) CustomFilter.OrFilters.Add(NewFilter.GetCopy());
			else CustomFilter.AndFilters.Add(NewFilter.GetCopy());
			OnPropertyChanged(nameof(CustomFilter));
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
				StaticHelpers.LogToFile(ex);
			}
			if (Filters != null) return;
			Filters = new ObservableCollection<CustomVnFilter>();
			foreach (UserlistStatus field in Enum.GetValues(typeof(UserlistStatus)))
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.UserlistStatus, field));
				cf.Name = $"Userlist: {field}";
				Filters.Add(cf);
			}
			foreach (WishlistStatus field in Enum.GetValues(typeof(WishlistStatus)))
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.WishlistStatus, field));
				cf.Name = $"Wishlist: {field}";
				Filters.Add(cf);
			}
			foreach (LengthFilterEnum field in Enum.GetValues(typeof(LengthFilterEnum)))
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.Length, field));
				cf.Name = $"Length: {field}";
				Filters.Add(cf);
			}
			foreach (ReleaseStatusEnum field in Enum.GetValues(typeof(ReleaseStatusEnum)))
			{
				var cf = new CustomVnFilter();
				cf.AndFilters.Add(new VnFilter(VnFilterType.ReleaseStatus, field));
				cf.Name = $"Release Status: {field}";
				Filters.Add(cf);
			}
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
