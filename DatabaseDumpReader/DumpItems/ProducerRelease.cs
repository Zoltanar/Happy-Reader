using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader.DumpItems
{
	/// <summary>
	/// File: releases
	/// </summary>
	public class Release : IDumpItem
	{
		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		public static Dictionary<string, int> Headers = new();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public int ReleaseId { get; set; }
		public string Released { get; set; }
		public string Website { get; set; }
		public List<LangRelease> Languages { get; set; }
		public List<int> Producers { get; set; }

		public void LoadFromStringParts(string[] parts)
		{
			ReleaseId = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			Released = GetPart(parts, "released");
			Website = GetPart(parts, "website");
		}
	}

	public class ProducerRelease : IDumpItem
	{
		public static Dictionary<string, int> Headers = new();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		public bool Developer { get; set; }

		public bool Publisher { get; set; }

		public int ProducerId { get; set; }

		public int ReleaseId { get; set; }

		public void LoadFromStringParts(string[] parts)
		{
			ReleaseId = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			ProducerId = Convert.ToInt32(GetPart(parts, "pid").Substring(1));
			Developer = GetPart(parts, "developer") == "t";
			Publisher = GetPart(parts, "publisher") == "t";
		}
	}

	public class VnRelease : IDumpItem
	{
		public static Dictionary<string, int> Headers = new();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		public void LoadFromStringParts(string[] parts)
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