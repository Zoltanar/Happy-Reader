using System;
using System.Collections.Generic;
using System.Linq;

namespace Happy_Apps_Core.Database;

public abstract class DumpItem
{
    private const string TrueValue = "t";
    private const string NullValue = "\\N";

    protected static Dictionary<string, int> Headers = new();


    /// <summary>
    /// Returns data as-is (e.g. "\N" when data is null as per database dump)
    /// </summary>
    protected string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

    /// <summary>
    /// Returns null when data is "\N", as per database dump.
    /// </summary>
    protected string GetPartOrNull(string[] parts, string columnName)
    {
        var result =  parts[Headers[columnName]];
        return result == NullValue ? null : result;
    }

    protected bool GetBoolean(string[] parts, string columnName) => parts[Headers[columnName]] == TrueValue;

    protected int GetInteger(string[] parts, string columnName, int skipCharacters = 0) => Convert.ToInt32(parts[Headers[columnName]].Substring(skipCharacters));

    /// <summary>
    /// If data is null, returns zero.
    /// </summary>
    protected double GetDouble(string[] parts, string columnName)
    {
        var part = parts[Headers[columnName]];
        return part == NullValue ? 0 : Convert.ToDouble(part);
    }

    /// <summary>
    /// Returns first string part where value is not null or \N, if both parts are null or \N, returns null.
    /// </summary>
    /// <remarks>We hard-code 2 columns (instead of using params) to prevent a string[] object being created every time this is called.</remarks>
    protected string GetFirstNonNullPart(string[] parts, string firstColumnName, string secondColumnName, out bool firstIsNull)
    {
        var part = GetPart(parts, firstColumnName);
        if (string.IsNullOrWhiteSpace(part) || part == "\\N")
        {
            part = GetPart(parts, secondColumnName);
            firstIsNull = true;
        }
        else firstIsNull = false;
        return part == "\\N" ? null : part;
    }

    public abstract void LoadFromStringParts(string[] parts);

    public virtual void SetDumpHeaders(string[] parts)
    {
        int colIndex = 0;
        Headers = parts.ToDictionary(c => c, _ => colIndex++);
    }
}