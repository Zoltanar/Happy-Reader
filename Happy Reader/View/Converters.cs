using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader.View
{
	public class LastPlayedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is DateTime dt)) return value;
			if (dt == DateTime.MinValue) return "Never";
			var timeSince = DateTime.Now - dt;
			if (timeSince.TotalDays < 3) return "Last 3 days";
			if (timeSince.TotalDays < 7) return "Last week";
			return timeSince.TotalDays < 30 ? "Last month" : "Earlier";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			new NotSupportedException();
	}

	public class TimeOpenConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is TimeSpan time)) return value;
			if (time == TimeSpan.Zero) return "Never";
			if (time.TotalMinutes < 1) return "<1 Minute";
			if (time.TotalHours < 0.5) return "<30 Minutes";
			if (time.TotalHours < 1) return "<1 Hour";
			if (time.TotalHours < 3) return "<3 Hours";
			if (time.TotalHours < 8) return "<8 Hours";
			if (time.TotalHours < 20) return "<20 Hours";
			if (time.TotalHours < 50) return "<50 Hours";
			return time.TotalHours < 100 ? "<100 Hours" : ">100 Hours";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			new NotSupportedException();
	}

	public class StringToNullableIntConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string text)) throw new NotSupportedException("Input must be a string.");
			if (string.IsNullOrWhiteSpace(text)) return (int?) null;
			return int.Parse(text);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
			new NotSupportedException($"From {nameof(StringToNullableIntConverter)}");
	}

	public class TagConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is null || !(value is string tag) || string.IsNullOrWhiteSpace(tag) ? "No Tag" : tag;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new NotSupportedException();
	}

	public class NullableToOpacityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null ? 0 : 1;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new NotSupportedException();
	}

	public class NullableToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null ? Visibility.Collapsed : Visibility.Visible;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new NotSupportedException();
	}

	public class NullableToBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new NotSupportedException();
	}

	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool val)) throw new NotSupportedException();
			return val ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}//✔️ //

	public class VnToSpecialFlagVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is ListedVN vn)) throw new NotSupportedException($"Value was type {value.GetType()}");
			var alert = vn.GetAlertFlag(StaticHelpers.CSettings.AlertTagIDs, StaticHelpers.CSettings.AlertTraitIDs);
			return alert ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}

	public class OwnedStatusToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is OwnedStatus val)) throw new NotSupportedException();
			return val == OwnedStatus.NeverOwned ? Visibility.Hidden : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}

	public class OwnedStatusToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is OwnedStatus val)) throw new NotSupportedException();
			return val switch
			{
				OwnedStatus.NeverOwned => Brushes.Transparent,
				OwnedStatus.PastOwned => Brushes.DarkKhaki,
				OwnedStatus.CurrentlyOwned => Brushes.Green,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class OwnedStatusToTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is OwnedStatus val)) throw new NotSupportedException();
			return val switch
			{
				OwnedStatus.NeverOwned => string.Empty,
				OwnedStatus.PastOwned => "❌",
				OwnedStatus.CurrentlyOwned => "✔️",
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class DateToWeightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is DateTime releaseDate)) throw new NotSupportedException();
			var now = DateTime.UtcNow.Date;
			return releaseDate.Date.Year == now.Year && releaseDate.Date.Month == now.Month ? FontWeights.Bold : FontWeights.Normal;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class DateToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return Brushes.Black;
			if (!(value is ListedVN vn)) throw new NotSupportedException($"Value was type {value.GetType()}");
			return vn.ReleaseDate > DateTime.UtcNow ? Theme.UnreleasedBrush : (vn.UserVN?.Blacklisted ?? false) ? Brushes.White : Brushes.Black;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class UserVnToBackgroundConverter : IValueConverter
	{
		/// <summary>
		/// If parameter is 1, the fallback brush is transparent, else it will be the default tile brush.
		/// </summary>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return parameter is string sP && sP == "1" ? Brushes.Transparent : Theme.DefaultTileBrush;
			if (!(value is UserVN userVN)) throw new NotSupportedException();
			var excludedLabels = new List<UserVN.LabelKind> { UserVN.LabelKind.Wishlist, UserVN.LabelKind.Voted };
			var label = userVN.Labels.FirstOrDefault(i => !excludedLabels.Contains(i));
			return label switch
			{
				UserVN.LabelKind.Playing => Theme.ULPlayingBrush,
				UserVN.LabelKind.Finished => Theme.ULFinishedBrush,
				UserVN.LabelKind.Stalled => Theme.ULStalledBrush,
				UserVN.LabelKind.Dropped => Theme.ULDroppedBrush,
				UserVN.LabelKind.Owned => Theme.ULUnknownBrush,
				UserVN.LabelKind.WishlistHigh => Theme.WLHighBrush,
				UserVN.LabelKind.WishlistMedium => Theme.WLMediumBrush,
				UserVN.LabelKind.WishlistLow => Theme.WLLowBrush,
				UserVN.LabelKind.Blacklist => Theme.WLBlacklistBrush,
				_ => parameter is string sP2 && sP2 == "1" ? Brushes.Transparent : Theme.DefaultTileBrush
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class UserVnToForegroundConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return Brushes.Black;
			if (!(value is UserVN userVN)) throw new NotSupportedException();
			return /*userVN.Labels.Contains(UserVN.LabelKind.Playing) ? Theme.ULPlayingBrush :*/ userVN.Blacklisted ? Brushes.White : Brushes.Black;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class VnToProducerForegroundConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return Brushes.White;
			if (!(value is ListedVN vn)) throw new NotSupportedException();
			return StaticHelpers.VNIsByFavoriteProducer(vn) ? Theme.FavoriteProducerBrush : (vn.UserVN?.Blacklisted ?? false) ? Brushes.White : Brushes.Black;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}