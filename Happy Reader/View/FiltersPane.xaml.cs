using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
	public partial class FiltersPane : UserControl
	{

		// ReSharper disable once NotAccessedField.Local
		private FiltersViewModelBase ViewModel => (FiltersViewModelBase)DataContext;
		private AutoCompleteBox _traitOrTagControl;
		
		public FiltersPane()
		{
			InitializeComponent();
		}

		private void FilterKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete) return;
			ListBox listBox = (ListBox)sender;
			var list = (System.Collections.IList)listBox.ItemsSource;
			var selected = listBox.SelectedItems;
			Array array = new object[selected.Count];
			selected.CopyTo(array, 0);
			foreach (var item in array) list.Remove(item);
			var parent = listBox.FindParent<GroupBox>();
			if (parent == PermanentFilterGroupBox) ViewModel.SavePermanentFilter();
		}

		private void SaveOrGroup(object sender, RoutedEventArgs e)
		{
			var parent = ((DependencyObject)sender).FindParent<GroupBox>();
			var isPermanent = parent == PermanentFilterGroupBox;
			ViewModel.SaveOrGroup(isPermanent);
		}

		private void DeleteCustomFilter(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteCustomFilter();
		}

		private void FilterTypeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ViewModel.SelectedFilterIndex < 0) return;
			FilterValuesGrid.Children.Clear();
			var value = (Enum)ViewModel.FilterTypes[ViewModel.SelectedFilterIndex].Tag;
			var type = value.GetConvertType();
			if (type == null) return;
			FrameworkElement control;
			DependencyProperty bindingProperty = null;
			var valueBinding = new Binding($"{nameof(ViewModel.NewFilter)}.{nameof(IFilter.Value)}"){Mode = BindingMode.OneWayToSource};
			if (type.IsEnum)
			{
				control = new ComboBox
				{
					ItemsSource = StaticMethods.GetEnumValues(type), 
					SelectedIndex = 0,
					SelectedValuePath = nameof(Tag)
				};
				bindingProperty = Selector.SelectedValueProperty;
			}
			else
			{
				if (type == typeof(bool)) control = new TextBlock(new System.Windows.Documents.Run("Use Exclude check box"));
				else if (type == typeof(string))
				{
					control = new TextBox();
					bindingProperty = TextBox.TextProperty;
				}
				else if (type == typeof(int))
				{
					control = new TextBox();
					bindingProperty = TextBox.TextProperty;
					control.PreviewTextInput += NumberValidationTextBox;
				}
				else if (type == typeof(DumpFiles.WrittenTag))
				{
					_traitOrTagControl = new AutoCompleteBox() { ItemFilter = TraitOrTagBoxFilter, ItemsSource = DumpFiles.GetAllTags() };
					control = _traitOrTagControl;
					bindingProperty = AutoCompleteBox.SelectedItemProperty;
				}
				else if (type == typeof(DumpFiles.WrittenTrait))
				{
					_traitOrTagControl = new AutoCompleteBox()
					{
						ItemFilter = TraitOrTagBoxFilter,
						HorizontalAlignment = HorizontalAlignment.Stretch,
						Margin = new Thickness(130 + 5, 2, 2, 2),
					};
					var rootControl = new ComboBox
					{
						ItemsSource = StaticMethods.GetEnumValues(typeof(DumpFiles.RootTrait)),
						SelectedIndex = 0,
						SelectedValuePath = nameof(Tag),
						HorizontalAlignment = HorizontalAlignment.Left,
						Width = 130,
					};

					rootControl.SelectionChanged += RootControl_SelectionChanged;
					FilterValuesGrid.Children.Add(rootControl);
					control = _traitOrTagControl;
					bindingProperty = AutoCompleteBox.SelectedItemProperty;
				}
				else control = new TextBlock(new System.Windows.Documents.Run(type.ToString()));
			}
			if(bindingProperty != null) control.SetBinding(bindingProperty, valueBinding);
			FilterValuesGrid.Children.Add(control);
		}
		
		private void RootControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0) return;
			var selectedTraitRoot = (DumpFiles.RootTrait)((ComboBoxItem)e.AddedItems[0]).Tag;
			_traitOrTagControl.ItemsSource = DumpFiles.GetTraitsForRoot(selectedTraitRoot);
		}
		
		private bool TraitOrTagBoxFilter(string input, object item)
		{
			//Short input is not filtered to prevent excessive loading times
			if (input.Length <= 2) return false;
			var trait = (DumpFiles.ItemWithParents)item;
			return trait.Name.ToLowerInvariant().Contains(input.ToLowerInvariant());
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}
	}
}
