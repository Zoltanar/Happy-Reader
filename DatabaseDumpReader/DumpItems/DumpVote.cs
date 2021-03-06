﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader.DumpItems
{
	public class DumpVote : IDumpItem
	{
		public void LoadFromStringParts(string[] parts)
		{
			parts = parts[0].Split(' ');
			VNId = Convert.ToInt32(GetPart(parts,"vid"));
			UserId = Convert.ToInt32(GetPart(parts, "uid"));
			Vote = Convert.ToInt32(GetPart(parts, "vote"));
			Date = DateTime.ParseExact(GetPart(parts, "date"), "yyyy-MM-dd",CultureInfo.InvariantCulture);
		}

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts) 
		{
			Headers = new Dictionary<string, int>
			{
				{ "vid",0},
				{ "uid",1},
				{ "vote",2},
				{ "date",3},
			};
		}


		public int Vote { get; set; }
		public int VNId { get; set; }
		public int UserId { get; set; }
		public DateTime Date { get; set; }
	}
}