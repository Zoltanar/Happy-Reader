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
	public abstract class FiltersViewModelBase<TFilter,T, TType> : IFiltersViewModel where TFilter : CustomFilter<T,TType>, new() where TType : Enum
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private TFilter _customFilter;
		private int _selectedFilterIndex;
		public abstract string PermanentFilterJsonFile { get; }
		public abstract string CustomFiltersJsonFile { get; }
		public ObservableCollection<TFilter> Filters { get; private set; }
		public TFilter PermanentFilter { get; set; }
		public TFilter CustomFilter
		{
			get => _customFilter;
			set
			{
				_customFilter = (TFilter) (value == null ? new TFilter() : value.GetCopy());
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

		public abstract IFilter<T, TType> NewFilter { get; }
		public ComboBoxItem[] FilterTypes => StaticMethods.GetEnumValues(typeof(TType));
		public ComboBoxItem[] LengthTypes { get; } = StaticMethods.GetEnumValues(typeof(LengthFilterEnum));
		public ComboBoxItem[] ReleaseTypes { get; } = StaticMethods.GetEnumValues(typeof(ReleaseStatusEnum));
		public ComboBoxItem[] Labels { get; } = StaticMethods.GetEnumValues(typeof(UserVN.LabelKind));
		public ComboBoxItem[] GameOwnedStatus { get; } = StaticMethods.GetEnumValues(typeof(OwnedStatus));

		public string SaveFilterError { get; set; }


		protected FiltersViewModelBase()
		{
			SelectedFilterIndex = 0;
			LoadPermanentFilter();
			LoadFilters();
			CustomFilter = Filters.FirstOrDefault();
			AddToCustomFilterCommand = new CommandHandler(AddToCustomFilter, true);
			AddToPermanentFilterCommand = new CommandHandler(AddToPermanentFilter, true);
			SaveCustomFilterCommand = new CommandHandler(SaveCustomFilter, true);
		}

		public void SaveCustomFilter()
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
				else
				{
					Filters.Add(CustomFilter);
					CustomFilter = new TFilter();
				}
				SaveFilters();
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
				var indexOfFilter = Filters.IndexOf((TFilter) CustomFilter.OriginalFilter);
				if (indexOfFilter == -1)
				{
					SaveFilterError = "Failed to find filter in collection.";
					return;
				}
				Filters.RemoveAt(indexOfFilter);
				CustomFilter = Filters.Count == 1
					? new TFilter()
					: indexOfFilter > 0 ? Filters[indexOfFilter - 1] : Filters[indexOfFilter + 1];
				SaveFilters();
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
			SavePermanentFilter();
		}

		public void AddToCustomFilter()
		{
			if (NewFilterOrGroup) CustomFilter.OrFilters.Add(NewFilter.GetCopy());
			else CustomFilter.AndFilters.Add(NewFilter.GetCopy());
			OnPropertyChanged(nameof(CustomFilter));
		}

		public void LoadPermanentFilter()
		{
			try
			{
				if (File.Exists(PermanentFilterJsonFile))
				{
					var text = File.ReadAllText(PermanentFilterJsonFile);
					PermanentFilter = JsonConvert.DeserializeObject<TFilter>(text, StaticMethods.SerialiserSettings);
				}
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			if (PermanentFilter != null) return;
			PermanentFilter = new TFilter();
		}

		public void SavePermanentFilter()
		{
			var text = JsonConvert.SerializeObject(PermanentFilter, Formatting.Indented, StaticMethods.SerialiserSettings);
			File.WriteAllText(PermanentFilterJsonFile, text);
			OnPropertyChanged(nameof(PermanentFilter));
		}

		public void LoadFilters()
		{
			try
			{
				if (File.Exists(CustomFiltersJsonFile))
				{
					var text = File.ReadAllText(CustomFiltersJsonFile);
					Filters = JsonConvert.DeserializeObject<ObservableCollection<TFilter>>(text, StaticMethods.SerialiserSettings);
				}
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			if (Filters != null) return;
			Filters = new ObservableCollection<TFilter>();
			SaveFilters();
		}

		public void SaveFilters()
		{
			var text = JsonConvert.SerializeObject(Filters, Formatting.Indented, StaticMethods.SerialiserSettings);
			File.WriteAllText(StaticMethods.CustomFiltersJson, text);
		}

		public void SaveOrGroup(bool isPermanent)
		{
			var filter = isPermanent ? PermanentFilter : CustomFilter;
			filter.SaveOrGroup();
			if (isPermanent) SavePermanentFilter();
		}

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}