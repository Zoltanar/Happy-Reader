using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader.DumpItems
{
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
			ReleaseId = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			Released = GetPart(parts, "released");
			Website = GetPart(parts, "website");
		}
	}

	public class ProducerRelease : DumpItem
	{
		public bool Developer { get; set; }

		public bool Publisher { get; set; }

		public int ProducerId { get; set; }

		public int ReleaseId { get; set; }

		public override void LoadFromStringParts(string[] parts)
		{
			ReleaseId = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			ProducerId = Convert.ToInt32(GetPart(parts, "pid").Substring(1));
			Developer = GetPart(parts, "developer") == "t";
			Publisher = GetPart(parts, "publisher") == "t";
		}
	}

	public class VnRelease : DumpItem
	{
        public override void LoadFromStringParts(string[] parts)
		{
			ReleaseId = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			VnId = Convert.ToInt32(GetPart(parts, "vid").Substring(1));
			ReleaseType = GetPart(parts, "rtype");
		}

		public int VnId { get; set; }

		public int ReleaseId { get; set; }

		public string ReleaseType { get; set; }
	}
}