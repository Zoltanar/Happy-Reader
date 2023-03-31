using Happy_Apps_Core.Database;

namespace DatabaseDumpReader.DumpItems
{
	public class DumpTitle : DumpItem
	{
		public override void LoadFromStringParts(string[] parts)
		{
			VNId = GetInteger(parts, "id",1);
			Lang = GetPart(parts, "lang");
			Title = GetPartOrNull(parts, "title");
			Latin = GetPartOrNull(parts, "latin");
			Official = GetBoolean(parts, "official");
		}
		
		public int VNId { get; set; }
		public string Lang { get; set; }
		public string Title { get; set; }
		public string Latin { get; set; }
		public bool Official { get; set; }
	}
}
