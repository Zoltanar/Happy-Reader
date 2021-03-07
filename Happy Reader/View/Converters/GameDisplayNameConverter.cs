using System;
using System.Globalization;
using System.Windows.Data;
using Happy_Reader.Database;

namespace Happy_Reader.View.Converters
{
	public class GameDisplayNameConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string gameName = value switch
			{
				null => "(none)",
				UserGame userGame => userGame.DisplayName,
				string sValue => string.IsNullOrWhiteSpace(sValue) ? "(none)" : sValue,
				_ => throw new NotSupportedException()
			};
			return $"Game: {gameName}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
