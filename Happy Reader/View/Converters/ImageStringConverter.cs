using System;
using System.Globalization;
using System.Windows.Data;

namespace Happy_Reader.View.Converters
{
	public class ImageStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is not string language
				? throw new NotSupportedException("Expected language string.")
				: StaticMethods.GetFlag(language);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
