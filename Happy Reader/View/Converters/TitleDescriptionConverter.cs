using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Happy_Reader.View.Converters
{
	public class TitleDescriptionConverter : IValueConverter
	{
		private static readonly Regex NewLineRegex = new(@"\\n", RegexOptions.Compiled);
		private static readonly Regex UrlRegex = new(@"(.*)\[url=[^];]*]([^[;]*)\[\/url](.*)", RegexOptions.Compiled);

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return null;
			if (value is not string sValue) throw new NotSupportedException();
			var result = NewLineRegex.Replace(sValue, Environment.NewLine);
			result = UrlRegex.Replace(result, "$1$2$3");
			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
