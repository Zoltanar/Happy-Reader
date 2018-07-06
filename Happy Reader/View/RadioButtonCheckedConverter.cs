using System;
using System.Windows.Data;

// ReSharper disable once CheckNamespace
namespace Happy_Reader
{
	public class RadioButtonCheckedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
			System.Globalization.CultureInfo culture)
		{
			return value?.Equals(parameter);
		}

		public object ConvertBack(object value, Type targetType, object parameter,
			System.Globalization.CultureInfo culture)
		{
			if (value == null) return null;
			return value.Equals(true) ? parameter : Binding.DoNothing;
		}
	}
}
