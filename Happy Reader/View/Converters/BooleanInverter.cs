using System;
using System.Globalization;
using System.Windows.Data;

namespace Happy_Reader.View.Converters
{
	class BooleanInverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not bool bValue) throw new NotSupportedException();
			return !bValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(targetType != typeof(bool)) throw new NotSupportedException();
			if (value is bool bValue) return !bValue;
			if (value is string sValue) return !bool.Parse(sValue);
			return !System.Convert.ToBoolean(value);
		}
	}
}
