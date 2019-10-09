using System;
using System.Globalization;
using System.Windows.Data;

namespace Happy_Reader.View
{
	public class TimeSpanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(!(value is TimeSpan ts) || targetType != typeof(string)) throw new NotImplementedException();
			return $"{(int) ts.TotalHours}:{ts.Minutes:00}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
