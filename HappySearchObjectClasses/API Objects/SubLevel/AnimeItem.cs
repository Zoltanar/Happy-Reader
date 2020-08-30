using System.Text;

namespace Happy_Apps_Core
{
		public class AnimeItem
		{
			public int ID { get; set; }
			public string RomajiTitle { get; set; }
			public string OriginalTitle { get; set; }
			public int Year { get; set; }
			public string Type { get; set; }

			public string Print()
			{
				var sb = new StringBuilder();
				if (RomajiTitle != null) sb.Append(RomajiTitle);
				else if (OriginalTitle != null) sb.Append(OriginalTitle);
				else sb.Append(ID);
				if (Year > 0) sb.Append($" ({Year})");
				if (Type != null) sb.Append($" ({Type})");
				return sb.ToString();
			}
		}
}