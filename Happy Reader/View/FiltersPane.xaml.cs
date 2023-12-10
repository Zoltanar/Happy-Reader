using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View
{
    public partial class FiltersPane : UserControl
    {
        // ReSharper disable once NotAccessedField.Local
        private FiltersViewModel ViewModel => (FiltersViewModel)DataContext;
        private AutoCompleteBox _traitOrTagControl;
        private DockPanel _languageDockPanel;
        private readonly ReleaseFilterPanel _releaseMonthGrid = new();
        private readonly LangRelease _langRelease = new();

        public FiltersPane()
        {
            InitializeComponent();
            CreateLangReleaseFilter();
        }

        private void CreateLangReleaseFilter()
        {
            _languageDockPanel = new DockPanel();
            CreateLangReleaseCheckBox("MTL", "Machine Translation", nameof(LangRelease.Mtl));
            CreateLangReleaseCheckBox("Partial", "Partial Release", nameof(LangRelease.Partial));
            var languageTextBox = new TextBox();
            languageTextBox.SourceUpdated += UpdateLangReleaseFilter;
            var languageBinding = new Binding(nameof(LangRelease.Lang));
            languageBinding.NotifyOnSourceUpdated = true;
            languageTextBox.SetBinding(TextBox.TextProperty, languageBinding);
            _languageDockPanel.Children.Add(languageTextBox);
            DockPanel.SetDock(languageTextBox, Dock.Right);
            languageTextBox.DataContext = _langRelease;
        }

        private void CreateLangReleaseCheckBox(string content, string tooltip, string property)
        {
            var checkBox = new CheckBox { Content = content, ToolTip = tooltip };
            checkBox.Checked += UpdateLangReleaseFilter;
            checkBox.Unchecked += UpdateLangReleaseFilter;
            checkBox.SetBinding(ToggleButton.IsCheckedProperty, property);
            _languageDockPanel.Children.Add(checkBox);
            DockPanel.SetDock(checkBox, Dock.Left);
            checkBox.DataContext = _langRelease;
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
            if (parent == PermanentFilterGroupBox) ViewModel.SaveToFile(true);
        }

        private void SaveOrGroup(object sender, RoutedEventArgs e)
        {
            var parent = ((DependencyObject)sender).FindParent<GroupBox>();
            var isPermanent = parent == PermanentFilterGroupBox;
            ViewModel.SaveOrGroup(isPermanent);
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
            var valueBinding = new Binding($"{nameof(ViewModel.NewFilter)}.{nameof(IFilter.Value)}") { Mode = BindingMode.OneWayToSource };
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
            else if (type == typeof(bool)) control = new TextBlock(new System.Windows.Documents.Run("Use Exclude check box"));
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
                _traitOrTagControl = new AutoCompleteBox() { ItemFilter = DumpfileFilter, ItemsSource = DumpFiles.GetAllTags() };
                control = _traitOrTagControl;
                bindingProperty = AutoCompleteBox.SelectedItemProperty;
            }
            else if (type == typeof(DumpFiles.WrittenTrait))
            {
                _traitOrTagControl = new AutoCompleteBox()
                {
                    ItemFilter = DumpfileFilter,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(130 + 5, 2, 2, 2),
                };
                var traitRootItems = StaticMethods.GetEnumValues(typeof(DumpFiles.RootTrait));
                traitRootItems[0].Content = "Any";
                traitRootItems[0].Tag = (DumpFiles.RootTrait)(-1);
                var rootControl = new ComboBox
                {
                    ItemsSource = traitRootItems,
                    SelectedValuePath = nameof(Tag),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 130
                };
                rootControl.SelectionChanged += RootControl_SelectionChanged;
                rootControl.SelectedIndex = 0;
                FilterValuesGrid.Children.Add(rootControl);
                control = _traitOrTagControl;
                bindingProperty = AutoCompleteBox.SelectedItemProperty;
            }
            else if (type == typeof(LangRelease))
            {
                control = _languageDockPanel;
                bindingProperty = DataContextProperty;
            }
            else if (type == typeof(ListedProducer))
            {
                _traitOrTagControl = new AutoCompleteBox() { ItemFilter = ProducerBoxFilter, ItemsSource = StaticHelpers.LocalDatabase.Producers };
                control = _traitOrTagControl;
                bindingProperty = AutoCompleteBox.SelectedItemProperty;
            }
            else if (type == typeof(StaffItem) || type == typeof(VnSeiyuu))
            {
                _traitOrTagControl = new AutoCompleteBox { ItemFilter = StaffItemFilter, ItemsSource = StaticHelpers.LocalDatabase.StaffAliases };
                control = _traitOrTagControl;
                bindingProperty = AutoCompleteBox.SelectedItemProperty;
            }
            else if (type == typeof(ReleaseDateFilter))
            {
                control = _releaseMonthGrid;
                _releaseMonthGrid.ViewModel = ViewModel.NewFilter;
            }
            else control = new TextBlock(new System.Windows.Documents.Run(type.ToString()));
            if (bindingProperty != null) control.SetBinding(bindingProperty, valueBinding);
            FilterValuesGrid.Children.Add(control);
        }

        private void UpdateLangReleaseFilter(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            ViewModel.NewFilter.Value = _langRelease;
        }

        private void RootControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            var selectedTraitRoot = (DumpFiles.RootTrait)((ComboBoxItem)e.AddedItems[0]).Tag;
            _traitOrTagControl.ItemsSource = selectedTraitRoot == (DumpFiles.RootTrait)(-1) ? DumpFiles.GetAllTraits() : DumpFiles.GetTraitsForRoot(selectedTraitRoot);
        }

        private bool DumpfileFilter(string input, object item)
        {
            //Short input is not filtered to prevent excessive loading times
            if (input.Length <= 2) return false;
            var trait = (DumpFiles.ItemWithParents)item;
            return trait.Name.ToLowerInvariant().Contains(input.ToLowerInvariant());
        }

        private bool ProducerBoxFilter(string input, object item)
        {
            //Short input is not filtered to prevent excessive loading times
            if (input.Length <= 2) return false;
            var producer = (ListedProducer)item;
            return producer.Name.ToLowerInvariant().Contains(input.ToLowerInvariant());
        }

        private bool StaffItemFilter(string input, object item)
        {
            //Short input is not filtered to prevent excessive loading times
            if (input.Length <= 2) return false;
            var staff = (StaffAlias)item;
            return staff.Name.ToLowerInvariant().Contains(input.ToLowerInvariant());
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private async void ApplyFilterClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.ApplyCurrentFilter();
        }

        private void PreviewDeleteFilter(object sender, CanExecuteRoutedEventArgs e)
        {
            var grid = (DataGrid)sender;
            if (e.Command != DataGrid.DeleteCommand) return;
            if (grid.SelectedItems.Count == 0) return;
            var message = "Are you sure you wish to delete ";
            if (grid.SelectedItems.Count > 1) message += $"{grid.SelectedItems.Count} filters?";
            else message += $"filter '{((CustomFilter)grid.SelectedItems[0]).Name}' ?";
            if (MessageBox.Show(message, StaticHelpers.ClientName, MessageBoxButton.YesNo) != MessageBoxResult.Yes) e.Handled = true;
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = (DataGridRow)sender;
            if (row is not { DataContext: CustomFilter filter }) return;
            ViewModel.SelectFilter(filter);
        }

        private void RowSelected(object sender, SelectedCellsChangedEventArgs e)
        {
            if (e.AddedCells.Count != 1) return;
            var filter = (CustomFilter)e.AddedCells[0].Item;
            ViewModel.CustomFilter = filter;
        }
    }
}
