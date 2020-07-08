using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader
{
	internal class UserVnLabel : IDumpItem
	{
		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public int UserId { get; set; }
		public int LabelId { get; set; }
		public int VnId { get; set; }

		public void LoadFromStringParts(string[] parts)
		{
			UserId = Convert.ToInt32(parts[0]);
			LabelId = Convert.ToInt32(parts[1]);
			VnId = Convert.ToInt32(parts[2]);
		}
	}
}