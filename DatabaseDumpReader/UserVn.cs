using System;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader
{
	public class UserVn : DumpItem
	{
		public int UserId { get; set; }
		public int VnId { get; set; }
		public DateTime Added { get; set; }
		public DateTime LastModified { get; set; }
		public string Notes { get; set; }
		public string LabelsString { get; set; }

		public override void LoadFromStringParts(string[] parts)
		{
			UserId = Convert.ToInt32(GetPart(parts, "uid").Substring(1));
			VnId = Convert.ToInt32(GetPart(parts, "vid").Substring(1));
			Added = Convert.ToDateTime(GetPart(parts, "added"));
			// ReSharper disable once StringLiteralTypo
			LastModified = Convert.ToDateTime(GetPart(parts, "lastmod"));
			var notes = GetPart(parts, "notes");
			Notes = notes == @"\N" ? null : notes;
            LabelsString = GetPart(parts, "labels");
        }
    }
}