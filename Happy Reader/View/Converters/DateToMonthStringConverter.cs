using System;
using System.Globalization;
using System.Windows.Data;

namespace Happy_Reader.View.Converters
{
	public class DateToMonthStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is DateTime dt)) return "Other";
			var newDt = new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
			return string.Format(CultureInfo.InvariantCulture, "{0:MMMM} {0:yyyy}", newDt);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
