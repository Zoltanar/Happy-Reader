using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

//Unused fields commented out to save memory
namespace Happy_Apps_Core.DumpReader;

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
    public List<ReleaseImage> Images { get; } = new List<ReleaseImage>();

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

public class ReleaseImage : DumpItem
{
public int ReleaseId { get; set; }
public int Image { get; set; }
public string Type { get; set; }
//public int Vid { get; set; }
public string[] Languages { get; set; }

public override void LoadFromStringParts(string[] parts)
{
    ReleaseId = GetInteger(parts, "id", 1);
    Image = GetInteger(parts, "img", 2);
    Type = GetPart(parts, "itype");
    var languagesString = GetPartOrNull(parts, "lang");
    if(languagesString != null) Languages = languagesString.Trim('{','}').Split(',').ToArray();
}
}