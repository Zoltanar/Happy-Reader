using System;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader.DumpItems
{
	public class VnTag : DumpItem
	{
        public int TagId { get; set; }

		public int VnId { get; set; }

		public int Vote { get; set; }

		public int? Spoiler { get; set; }

		public bool Ignore { get; set; }

		public override void LoadFromStringParts(string[] parts)
		{
			TagId = Convert.ToInt32(GetPart(parts, "tag").Substring(1));
			VnId = Convert.ToInt32(GetPart(parts, "vid").Substring(1));
			Vote = Convert.ToInt32(GetPart(parts, "vote"));
			var spoiler = GetPart(parts, "spoiler");
			Spoiler = spoiler == "\\N" ? (int?)null : Convert.ToInt32(spoiler);
			Ignore = GetPart(parts, "ignore") == "t";
		}
	}
}