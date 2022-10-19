using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Happy_Reader.View.Converters
{
	public class ScoreConverter : IValueConverter
	{
		public static readonly ScoreConverter Instance = new();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DesignerProperties.GetIsInDesignMode(App.Current.MainWindow)) return value;
            if (value is null || value.Equals(System.Windows.DependencyProperty.UnsetValue)) return "None";
			if (value is not IConvertible voteObject) throw new NotSupportedException();
			var vote = voteObject.ToDouble(culture);
			if (StaticMethods.Settings.GuiSettings.UseDecimalVoteScores)
			{
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				return vote == 100 ? "10" : (vote / 10).ToString("0.0");
			}
			return vote.ToString("#00");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
