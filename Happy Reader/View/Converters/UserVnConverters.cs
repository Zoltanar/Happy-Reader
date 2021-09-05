using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
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

	public class UserRelatedStatusConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not UserVN userVN) return string.Empty;
			var sb = new StringBuilder();
			var label = userVN.Labels.FirstOrDefault(l => l != UserVN.LabelKind.Voted && l != UserVN.LabelKind.Wishlist);
			if (label != default)
			{
				sb.Append(label.GetDescription());
				if (userVN.Vote > 0) sb.Append($" (Vote: {ScoreConverter.Instance.Convert(userVN.Vote, targetType, parameter, culture)})");
			}
			else if (userVN.Vote > 0) sb.Append($"Vote: {ScoreConverter.Instance.Convert(userVN.Vote, targetType, parameter, culture)}");
			return sb.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

	public class UserRelatedTooltipConverter : IValueConverter
	{
		private const string DateFormat = "yyyy-MMM-dd";
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not UserVN userVN) return string.Empty;
			var sb = new StringBuilder();
			var label = userVN.Labels.FirstOrDefault(l => l != UserVN.LabelKind.Voted && l != UserVN.LabelKind.Wishlist);
			// ReSharper disable PossibleInvalidOperationException values are present based on label or vote presence
			if (label != default)
			{
				sb.Append($"Added: {userVN.Added.Value.ToString(DateFormat)}");
				if (userVN.LastModified != userVN.Added) sb.Append($" Modified: {userVN.LastModified.Value.ToString(DateFormat)}");
				if (userVN.Vote > 0) sb.Append($" (Vote Added: {userVN.VoteAdded.Value.ToString(DateFormat)})");
			}
			else if (userVN.Vote > 0) sb.Append($"Vote Added: {userVN.VoteAdded.Value.ToString(DateFormat)}");
			// ReSharper restore PossibleInvalidOperationException
			return sb.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}

}
