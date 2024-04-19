using System;
using Happy_Apps_Core.Database;

namespace Happy_Apps_Core.DumpReader;

public class VnTag : DumpItem
{
    public int TagId { get; set; }
    public int VnId { get; set; }
    public int Vote { get; set; }
    public int? Spoiler { get; set; }
    public bool Ignore { get; set; }

    public override void LoadFromStringParts(string[] parts)
    {
        TagId = GetInteger(parts, "tag", 1);
        VnId = GetInteger(parts, "vid", 1);
        Vote = GetInteger(parts, "vote");
        var spoiler = GetPartOrNull(parts, "spoiler");
        Spoiler = spoiler == null ? null : Convert.ToInt32(spoiler);
        Ignore = GetBoolean(parts, "ignore");
    }
}