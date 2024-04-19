using System;
using System.Collections.Generic;
using System.Globalization;
using Happy_Apps_Core.Database;

namespace Happy_Apps_Core.DumpReader;

public class DumpVote : DumpItem
{
    public override void LoadFromStringParts(string[] parts)
    {
        parts = parts[0].Split(' ');
        VNId = GetInteger(parts, "vid");
        UserId = GetInteger(parts, "uid");
        Vote = GetInteger(parts, "vote");
        Date = DateTime.ParseExact(GetPart(parts, "date"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public override void SetDumpHeaders(string[] parts)
    {
        Headers = new Dictionary<string, int>
        {
            { "vid",0},
            { "uid",1},
            { "vote",2},
            { "date",3},
        };
    }

    public int Vote { get; set; }
    public int VNId { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }
}