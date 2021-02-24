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

		public static readonly Dictionary<string, string> RelationDict = new()
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

		public static readonly Dictionary<string, string> Relation2Dict = new()
		{
			{"seq", "Prequel/Sequel"},
			{"preq", "Prequel/Sequel"},
			{"set", "Same Setting"},
			{"alt", "Alternative Version"},
			{"char", "Shares Characters"},
			{"side", "Side Story"},
			{"par", "Parent Story/Fandisc"},
			{"ser", "Same Series"},
			{"fan", "Parent Story/Fandisc"},
			{"orig", "Original Game"}
		};

		public string Print() => $"{(Official ? "" : "[Unofficial] ")}{RelationDict[Relation]} - {Title} - {ID}";

		public string Print2() => $"{(Official ? "" : "[Unofficial] ")}{Relation2Dict[Relation]} - {Title} - {ID}";

		public override string ToString() => $"ID={ID} Title={Title}";

		private sealed class IDEqualityComparer : IEqualityComparer<RelationsItem>
		{
			public bool Equals(RelationsItem x, RelationsItem y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				if (x.GetType() != y.GetType()) return false;
				return x.ID == y.ID;
			}

			public int GetHashCode(RelationsItem obj)
			{
				return obj.ID;
			}
		}

		public static IEqualityComparer<RelationsItem> IDComparer { get; } = new IDEqualityComparer();
	}
}