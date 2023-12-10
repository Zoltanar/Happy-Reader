using System;
using System.Text.RegularExpressions;

namespace Happy_Reader;

public class ReleaseDateFilter
{
    private static readonly Regex ParseRegex = new(@"(?<relative>(|Relative:))(?<year>Y-?\d{1,4})(?<month>M-?\d{1,2})?(?<day>D-?\d{1,2})?(?<between>;(?<torelative>(|Relative:))(?<toyear>Y-?\d{1,4})(?<tomonth>M-?\d{1,2})?(?<today>D-?\d{1,2})?)?");

    public bool Relative { get; set; }
    public int Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
    public bool Between { get; set; }
    public bool ToRelative { get; set; }
    public int ToYear { get; set; }
    public int? ToMonth { get; set; }
    public int? ToDay { get; set; }

    public bool IsInReleaseMonth(DateTime dateTime)
    {
        var fromOrOn = GetDateTime(true, Relative, Year, Month, Day);
        if (!Between)
        {
            if (dateTime.Year != fromOrOn.Year) return false;
            return (!Month.HasValue || dateTime.Month == fromOrOn.Month) && (!Day.HasValue || dateTime.Day == fromOrOn.Day);
        }
        var to = GetDateTime(false, ToRelative, ToYear, ToMonth, ToDay);
        return dateTime >= fromOrOn && dateTime <= to;
    }

    private static DateTime GetDateTime(bool from, bool relative, int year, int? month, int? day)
    {
        DateTime dateTime;
        if (!relative)
        {
            var actualMonth = month ?? (from ? 1 : 12);
            dateTime = new DateTime(year, actualMonth, day ?? (from ? 1 : DateTime.DaysInMonth(year, actualMonth)));
        }
        else
        {
            dateTime = DateTime.Today;
            dateTime = dateTime.AddYears(year);
            dateTime = month.HasValue ? dateTime.AddMonths(month.Value) : new DateTime(dateTime.Year, from ? 1 : 12, 1);
            dateTime = day.HasValue ? dateTime.AddDays(day.Value) : new DateTime(dateTime.Year, dateTime.Month, from ? 1 : DateTime.DaysInMonth(dateTime.Year, dateTime.Month));
        }
        return dateTime;

    }

    public override string ToString()
    {
        var result = $"{(Relative ? "Relative:" : string.Empty)}Y{Year:0000}";
        if (Month.HasValue) result += $"M{Month:00}";
        if (Day.HasValue) result += $"D{Day:00}";
        if (Between)
        {
            result += $";{(ToRelative ? "Relative:" : string.Empty)}Y{ToYear:0000}";
            if (ToMonth.HasValue) result += $"M{ToMonth:00}";
            if (ToDay.HasValue) result += $"D{ToDay:00}";
        }
        return result;
    }

    public static bool TryParse(string value, out ReleaseDateFilter releaseDateFilter)
    {
        releaseDateFilter = default;
        if (string.IsNullOrEmpty(value)) return false;
        var match = ParseRegex.Match(value);
        if (!match.Success) return false;
        releaseDateFilter = new ReleaseDateFilter
        {
            Relative = match.Groups["relative"].Value != string.Empty,
            Year = int.Parse(match.Groups["year"].Value.Substring(1)),
            Month = match.Groups["month"].Success ? int.Parse(match.Groups["month"].Value.Substring(1)) : null,
            Day = match.Groups["day"].Success ? int.Parse(match.Groups["day"].Value.Substring(1)) : null
        };
        if (!match.Groups["between"].Success) return true;
        releaseDateFilter.Between = true;
        releaseDateFilter.ToRelative = match.Groups["torelative"].Value != string.Empty;
        releaseDateFilter.ToYear = int.Parse(match.Groups["toyear"].Value.Substring(1));
        releaseDateFilter.ToMonth = match.Groups["tomonth"].Success ? int.Parse(match.Groups["tomonth"].Value.Substring(1)) : null;
        releaseDateFilter.ToDay = match.Groups["today"].Success ? int.Parse(match.Groups["today"].Value.Substring(1)) : null;
        return true;
    }
}