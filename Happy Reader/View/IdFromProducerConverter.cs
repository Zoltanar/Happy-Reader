using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Happy_Apps_Core.Database;

namespace Happy_Reader.View
{
    internal class IdFromProducerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return 0;
            var producer = (ListedProducer)value;
            return producer.ID;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}