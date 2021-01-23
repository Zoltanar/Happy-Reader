using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;

namespace Happy_Reader.View.Converters
{
	public class UserVnToBackgroundConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return Theme.DefaultTileBrush;
			if (!(value is UserVN userVN)) throw new NotSupportedException();
			var label = userVN.PriorityLabel;
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
				_ => Theme.DefaultTileBrush
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class UserVnToLabelConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null || value == DependencyProperty.UnsetValue) return "N/A";
			var label = value switch
			{
				UserVN userVN => userVN.PriorityLabel,
				UserVN.LabelKind vLabel => vLabel,
				_ => throw new NotSupportedException()
			};
			return label == default ? "N/A" : label.GetDescription();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class UserVnToForegroundConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return Brushes.Black;
			var userVN = value switch
			{
				UserVN vUserVN => vUserVN,
				ListedVN listedVN => listedVN.UserVN,
				CharacterItem character => character.VisualNovel?.UserVN,
				_ => throw new NotSupportedException()
			};
			return userVN?.Blacklisted ?? false ? Brushes.White : Brushes.Black;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class UserVnToScoreConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null || value == DependencyProperty.UnsetValue) return "N/A";
			int? vote = value switch
			{
				UserVN userVN => userVN.Vote,
				int vVote => vVote,
				_ => throw new NotSupportedException()
			};
			return vote.HasValue ? $"Vote: {vote}" : "N/A";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

}
