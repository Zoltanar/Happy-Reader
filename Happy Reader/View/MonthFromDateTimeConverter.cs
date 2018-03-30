using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Happy_Reader.View
{
    internal class MonthFromDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return DateTime.MaxValue;
            var dt = (DateTime) value;
            return new DateTime(dt.Year,dt.Month,DateTime.DaysInMonth(dt.Year,dt.Month));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}