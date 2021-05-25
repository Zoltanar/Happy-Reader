using System;
using System.Globalization;
using System.Windows.Data;
using Happy_Reader.Database;
using System.Text.RegularExpressions;

namespace Happy_Reader.View.Converters
{
	public class EntryGameConverter : IValueConverter
	{
		private static readonly Regex EntryGameRegex = new(@"[vu]\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value switch
			{
				EntryGame when targetType == typeof(string) => value.ToString(),
				null => EntryGame.None.ToString(),
				_ => throw new NotSupportedException()
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null) return EntryGame.None;
			if (value is not string sValue || targetType != typeof(EntryGame)) throw new NotSupportedException();
			int gameId;
			bool isUserGame;
			if (EntryGameRegex.IsMatch(sValue))
			{
				isUserGame = sValue.StartsWith("u", StringComparison.OrdinalIgnoreCase);
				gameId = int.Parse(sValue.Substring(1));
			}
			else
			{
				if (sValue == string.Empty || sValue.Equals("(No Game)")) return EntryGame.None;
				isUserGame = !sValue.Substring(1).StartsWith("VN");
				var openBracket = sValue.IndexOf('[');
				var closeBracket = sValue.IndexOf(']');
				if (closeBracket < openBracket || openBracket == -1 || closeBracket == -1) return EntryGame.None;
				var idString = sValue.Substring(openBracket + 1, closeBracket - openBracket - 1);
				var parsed = int.TryParse(idString, out gameId);
				if (!parsed) return EntryGame.None;
			}
			return new EntryGame(gameId, isUserGame, false);
		}
	}
}