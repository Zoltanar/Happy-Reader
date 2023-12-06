using Happy_Reader.Model.VnFilters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
namespace Happy_Reader.View
{
    public partial class ReleaseFilterPanel : UserControl
    {
        private readonly ReleaseMonthFilter _releaseMonth = new();
        public IFilter ViewModel { get; set; }

        public ReleaseFilterPanel()
        {
            InitializeComponent();
            //MainGrid.DataContext = _releaseMonth;
            var grid = MainGrid;
            var relativeCheckBox = new CheckBox() { Content = "Relative", DataContext = _releaseMonth };
            relativeCheckBox.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(ReleaseMonthFilter.Relative)) { NotifyOnSourceUpdated = true });
            relativeCheckBox.Checked += UpdateReleaseMonthFilter;
            relativeCheckBox.Unchecked += UpdateReleaseMonthFilter;
            relativeCheckBox.SourceUpdated += UpdateReleaseMonthFilter;
            var yearTextBox = new LabeledTextBox { Label = "Year", DataContext = _releaseMonth };
            yearTextBox.SetBinding(LabeledTextBox.TextProperty,
                new Binding(nameof(ReleaseMonthFilter.Year)) { NotifyOnSourceUpdated = true });
            yearTextBox.SourceUpdated += UpdateReleaseMonthFilter;
            var monthTextBox = new LabeledTextBox { Label = "Month", DataContext = _releaseMonth };
            monthTextBox.SetBinding(LabeledTextBox.TextProperty,
                new Binding(nameof(ReleaseMonthFilter.Month)) { NotifyOnSourceUpdated = true });
            monthTextBox.SourceUpdated += UpdateReleaseMonthFilter;
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.Children.Add(relativeCheckBox);
            Grid.SetColumn(relativeCheckBox, 0);
            grid.Children.Add(yearTextBox);
            Grid.SetColumn(yearTextBox, 1);
            grid.Children.Add(monthTextBox);
            Grid.SetColumn(monthTextBox, 2);
        }

        private void UpdateReleaseMonthFilter(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            ViewModel.Value = _releaseMonth;
        }
    }
}
