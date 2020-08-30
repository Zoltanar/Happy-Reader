using System.Collections.Generic;

namespace Happy_Apps_Core
{
	public class RelationsItem
	{
		public int ID { get; set; }
		public string Relation { get; set; }
		public string Title { get; set; }
		public string Original { get; set; }
		public bool Official { get; set; }

		public static readonly Dictionary<string, string> RelationDict = new Dictionary<string, string>
		{
			{"seq", "Sequel"},
			{"preq", "Prequel"},
			{"set", "Same Setting"},
			{"alt", "Alternative Version"},
			{"char", "Shares Characters"},
			{"side", "Side Story"},
			{"par", "Parent Story"},
			{"ser", "Same Series"},
			{"fan", "Fandisc"},
			{"orig", "Original Game"}
		};

		public string Print() => $"{(Official ? "" : "[Unofficial] ")}{RelationDict[Relation]} - {Title} - {ID}";

		public override string ToString() => $"ID={ID} Title={Title}";
	}
}