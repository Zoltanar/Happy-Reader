using System.Collections.Generic;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader.DumpItems;

/// <summary>
/// File: releases
/// </summary>
public class Release : DumpItem
{
    public int ReleaseId { get; set; }
    public string Released { get; set; }
    public string Website { get; set; }
    public List<LangRelease> Languages { get; set; }
    public List<int> Producers { get; set; }

    public override void LoadFromStringParts(string[] parts)
    {
        ReleaseId = GetInteger(parts, "id", 1);
        Released = GetPart(parts, "released");
        Website = GetPart(parts, "website");
    }
}

public class ProducerRelease : DumpItem
{
    public bool Developer { get; set; }
    //public bool Publisher { get; set; }
    public int ProducerId { get; set; }
    public int ReleaseId { get; set; }

    public override void LoadFromStringParts(string[] parts)
    {
        ReleaseId = GetInteger(parts, "id", 1);
        ProducerId = GetInteger(parts, "pid", 1);
        Developer = GetBoolean(parts, "developer");
        //Publisher = GetBoolean(parts, "publisher");
    }
}

public class VnRelease : DumpItem
{
    public override void LoadFromStringParts(string[] parts)
    {
        ReleaseId = GetInteger(parts, "id", 1);
        VnId = GetInteger(parts, "vid", 1);
        ReleaseType = GetPart(parts, "rtype");
    }

    public int VnId { get; set; }
    public int ReleaseId { get; set; }
    public string ReleaseType { get; set; }
}