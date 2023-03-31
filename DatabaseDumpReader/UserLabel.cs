using System;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader;

public class UserLabel : DumpItem
{
    public int UserId { get; set; }
    public int LabelId { get; set; }
    public UserVN.LabelKind Label { get; set; }

    public override void LoadFromStringParts(string[] parts)
    {
        UserId = GetInteger(parts, "uid", 1);
        LabelId = GetInteger(parts, "id");
        var label = GetPart(parts, "label").Replace("-", "");
        Label = Enum.IsDefined(typeof(UserVN.LabelKind), label) ? (UserVN.LabelKind)Enum.Parse(typeof(UserVN.LabelKind), label) : 0;
    }
}