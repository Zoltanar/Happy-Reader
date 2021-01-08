using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace Happy_Reader.ViewModel
{
	public interface IFiltersViewModel : INotifyPropertyChanged
	{
		event PropertyChangedEventHandler PropertyChanged;
		string PermanentFilterJsonFile { get; }
		string CustomFiltersJsonFile { get; }
		ICommand AddToCustomFilterCommand { get; set; }
		ICommand AddToPermanentFilterCommand { get; set; }
		ICommand SaveCustomFilterCommand { get; set; }
		bool NewFilterOrGroup { get; set; }
		int SelectedFilterIndex { get; set; }
		ComboBoxItem[] FilterTypes { get; }
		ComboBoxItem[] LengthTypes { get; }
		ComboBoxItem[] ReleaseTypes { get; }
		ComboBoxItem[] Labels { get; }
		ComboBoxItem[] GameOwnedStatus { get; }
		string SaveFilterError { get; set; }
		void SaveCustomFilter();
		void DeleteCustomFilter();
		void AddToPermanentFilter();
		void AddToCustomFilter();
		void LoadPermanentFilter();
		void SavePermanentFilter();
		void LoadFilters();
		void SaveFilters();
		void SaveOrGroup(bool isPermanent);
		void OnPropertyChanged([CallerMemberName] string propertyName = null);
	}
}