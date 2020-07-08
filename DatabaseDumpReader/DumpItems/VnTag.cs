using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader.DumpItems
{
	public class VnTag : IDumpItem
	{
		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public int TagId { get; set; }

		public int VnId { get; set; }

		public int Vote { get; set; }

		public int? Spoiler { get; set; }

		public bool Ignore { get; set; }

		public void LoadFromStringParts(string[] parts)
		{
			TagId = Convert.ToInt32(parts[0]);
			VnId = Convert.ToInt32(parts[1]);
			Vote = Convert.ToInt32(parts[3]);
			Spoiler = parts[4] == "\\N" ? (int?) null : Convert.ToInt32(parts[4]);
			Ignore = parts[6] == "t";
		}
	}
}