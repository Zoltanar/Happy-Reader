using System;
using System.Globalization;
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
			if (string.IsNullOrWhiteSpace(text)) return null;
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
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value == null || value is string sValue && string.IsNullOrWhiteSpace(sValue) ? Visibility.Collapsed : Visibility.Visible;

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

	public class VnOrCharacterToAlertFlagVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool alertFlag = value switch
			{
				ListedVN vn => vn.GetAlertFlag(StaticHelpers.CSettings.AlertTagIDs, StaticHelpers.CSettings.AlertTraitIDs),
				CharacterItem ch => ch.GetAlertFlag(StaticHelpers.CSettings.AlertTraitIDs),
				_ => throw new NotSupportedException($"Object {value} is of unsupported type {value?.GetType()}")
			};
			return alertFlag ? Visibility.Visible : Visibility.Hidden;
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

	public class CharacterToBackBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return Theme.DefaultTileBrush;
			if (!(value is CharacterItem character)) throw new NotSupportedException();
			return character.CharacterVN?.Role switch
			{
				"main" => Brushes.Gold,
				"primary" => Brushes.Orchid,
				"side" => Brushes.GreenYellow,
				"appears" => Brushes.LightBlue,
				null => Brushes.Gray,
				_ => Brushes.White
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class VnOrScreenToImagePathConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return Theme.ImageNotFoundImage;
			return value switch
			{
				ListedVN vn => vn.ImageNSFW && !StaticMethods.ShowNSFWImages() ? Theme.NsfwImage : (object)vn.ImageSource ?? Theme.ImageNotFoundImage,
				ScreenItem screen => screen.Nsfw && !StaticMethods.ShowNSFWImages() ? Theme.NsfwImage : (object)screen.StoredLocation ?? Theme.ImageNotFoundImage,
				_ => throw new NotSupportedException()
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class StringAndBracketedConverter : IMultiValueConverter
	{
		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null || value.Length != 2) return Theme.ImageNotFoundImage;
			if (value[1] == null || value[1] is string v1String && string.IsNullOrWhiteSpace(v1String)) return value[0];
			if (value[0] == null || value[0] is string v0String && string.IsNullOrWhiteSpace(v0String)) return value[1];
			return $"{value[0]} ({value[1]})";
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}