using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader
{
	public class UserVn : IDumpItem
	{
		public static Dictionary<string, int> Headers = new();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		public int UserId { get; set; }
		public int VnId { get; set; }
		public DateTime Added { get; set; }
		public string Notes { get; set; }
		public List<UserVN.LabelKind> Labels { get; } = new();

		public void LoadFromStringParts(string[] parts)
		{
			UserId = Convert.ToInt32(parts[0].Substring(1));
			VnId = Convert.ToInt32(parts[1].Substring(1));
			Added = Convert.ToDateTime(parts[2]);
			Notes = parts[7] == @"\N" ? null : parts[7];
		}
	}
}