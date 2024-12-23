﻿using System;
using Happy_Apps_Core.Database;

namespace Happy_Apps_Core.DumpReader;

public class UserVn : DumpItem
{
    public int UserId { get; set; }
    public int VnId { get; set; }
    public DateTime Added { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime? Started { get; set; }
    public DateTime? Finished { get; set; }
    public string Notes { get; set; }
    public string LabelsString { get; set; }

    public override void LoadFromStringParts(string[] parts)
    {
        UserId = GetInteger(parts, "uid", 1);
        VnId = GetInteger(parts, "vid", 1);
        Added = Convert.ToDateTime(GetPart(parts, "added"));
        // ReSharper disable once StringLiteralTypo
        LastModified = Convert.ToDateTime(GetPart(parts, "lastmod"));
        Started = GetNullableDateTime(parts, "started");
        Finished = GetNullableDateTime(parts, "finished");
        Notes = GetPartOrNull(parts, "notes");
        LabelsString = GetPart(parts, "labels");
    }
}