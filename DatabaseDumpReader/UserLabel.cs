using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

namespace DatabaseDumpReader
{
	public class UserLabel : DumpItem
	{
		public int UserId { get; set; }
		public int LabelId { get; set; }
		public UserVN.LabelKind Label { get; set; }

		public override void LoadFromStringParts(string[] parts)
		{
			UserId = Convert.ToInt32(parts[0].Substring(1));
			LabelId = Convert.ToInt32(parts[1]);
			var label = parts[2].Replace("-", "");
			Label = Enum.IsDefined(typeof(UserVN.LabelKind), label) ? (UserVN.LabelKind)Enum.Parse(typeof(UserVN.LabelKind), label) : 0;
		}
	}
}