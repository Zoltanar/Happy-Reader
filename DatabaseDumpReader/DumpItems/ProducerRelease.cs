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
			Headers = parts.ToDictionary(c=>c,c=>colIndex++);
		}

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public int ReleaseId { get; set; }
		public string Type { get; set; }
		public string Released { get; set; }
		public string Website { get; set; }
		public List<string> Languages { get; set; }
		public List<int> Producers { get; set; }

		public void LoadFromStringParts(string[] parts)
		{
			ReleaseId = Convert.ToInt32(GetPart(parts,"id"));
			Type = GetPart(parts, "type");
			Released = GetPart(parts, "released");
			Website = GetPart(parts, "website");
		}
	}

	public class ProducerRelease : IDumpItem
	{
		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public bool Developer { get; set; }

		public bool Publisher { get; set; }

		public int ProducerId { get; set; }

		public int ReleaseId { get; set; }

		public void LoadFromStringParts(string[] parts)
		{
			ReleaseId = Convert.ToInt32(GetPart(parts, "id"));
			ProducerId = Convert.ToInt32(GetPart(parts, "pid"));
			Developer = GetPart(parts, "developer") == "t";
			Publisher = GetPart(parts, "publisher") == "t";
		}
	}

	public class VnRelease : IDumpItem
	{
		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public void LoadFromStringParts(string[] parts)
		{
			ReleaseId = Convert.ToInt32(GetPart(parts, "id"));
			VnId = Convert.ToInt32(GetPart(parts, "vid"));
		}

		public int VnId { get; set; }

		public int ReleaseId { get; set; }
	}

	public class LangRelease : IDumpItem
	{
		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public void LoadFromStringParts(string[] parts)
		{
			ReleaseId = Convert.ToInt32(GetPart(parts, "id"));
			Lang = GetPart(parts, "lang");
		}

		public string Lang { get; set; }

		public int ReleaseId { get; set; }
	}
}