using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader.DumpItems
{
	public class DumpTitle : IDumpItem
	{
		public void LoadFromStringParts(string[] parts)
		{
			VNId = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			Lang = GetPart(parts, "lang");
			Title = GetPart(parts, "title");
			if (Title == "\\N") Title = null;
			Latin = GetPart(parts, "latin");
			if (Latin == "\\N") Latin = null;
			Official = GetPart(parts, "official") == "t";
		}

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}
		
		public int VNId { get; set; }
		public string Lang { get; set; }
		public string Title { get; set; }
		public string Latin { get; set; }
		public bool Official { get; set; }
	}
}
