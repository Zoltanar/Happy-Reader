﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Happy_Reader.Database;

namespace Happy_Reader.View.Converters
{
	public class RunningStatusToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return Brushes.Transparent;
			if (value is not UserGame.ProcessStatus runningStatus) throw new NotSupportedException();
			return runningStatus switch
			{
				UserGame.ProcessStatus.Off => Theme.ProcessOffBackground,
				UserGame.ProcessStatus.Paused => Theme.ProcessPausedBackground,
				UserGame.ProcessStatus.On => Theme.ProcessOnBackground,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
