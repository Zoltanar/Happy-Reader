using System;
using System.Text.RegularExpressions;

namespace Happy_Reader.Model.VnFilters;

public class ReleaseMonthFilter
{
    private static readonly Regex ParseRegex = new(@"(?<relative>(|Relative:))(?<year>-?\d{1,4}):(?<month>-?\d{1,2})");

    public bool Relative { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public bool IsInReleaseMonth(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        if (Relative)
        {
            var yearDiff = dateTime.Year - now.Year;
            if (yearDiff != Year) return false;
            var monthDiff = dateTime.Month - now.Month;
            return monthDiff == Month;
        }
        if (dateTime.Year != Year) return false;
        return dateTime.Month == Month;
    }

    public override string ToString() => $"{(Relative ? "Relative:" : string.Empty)}{Year:0000}:{Month:00}";

    public static bool TryParse(string value, out ReleaseMonthFilter releaseMonthFilter)
    {
        releaseMonthFilter = default;
        var match = ParseRegex.Match(value);
        if (!match.Success) return false;
        releaseMonthFilter = new ReleaseMonthFilter
        {
            Relative = match.Groups["relative"].Value != string.Empty,
            Year = int.Parse(match.Groups["year"].Value),
            Month = int.Parse(match.Groups["month"].Value)
        };
        return true;
    }
}