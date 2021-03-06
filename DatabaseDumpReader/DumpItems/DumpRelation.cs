﻿using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
// ReSharper disable InconsistentNaming

namespace DatabaseDumpReader.DumpItems
{
	public class DumpRelation : IDumpItem
	{
		void IDumpItem.LoadFromStringParts(string[] parts)
		{
			Id = Convert.ToInt32(GetPart(parts, "id"));
			VnId = Convert.ToInt32(GetPart(parts, "vid"));
			Relation = GetPart(parts, "relation");
			Official = GetPart(parts, "official") == "t";
		}

		public bool Official { get; set; }

		public string Relation { get; set; }

		public int VnId { get; set; }

		public int Id { get; set; }

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public RelationsItem ToRelationItem()
		{
			return new RelationsItem
			{
				ID = VnId,
				Relation = Relation,
				Official = Official
			};
		}
	}

	public class DumpAnime : IDumpItem
	{
		void IDumpItem.LoadFromStringParts(string[] parts)
		{
			Id = Convert.ToInt32(GetPart(parts, "id"));
			var yearString = GetPart(parts, "year");
			Year = yearString == "\\N" ? -1 : Convert.ToInt32(yearString);
			Title_Kanji = GetPart(parts, "title_kanji");
			Title_Romaji = GetPart(parts, "title_romaji");
		}

		public int Id { get; set; }
		public int Year { get; set; }
		public string Title_Romaji { get; set; }
		public string Title_Kanji { get; set; }

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}
	}

	public class DumpVnAnime : IDumpItem
	{
		void IDumpItem.LoadFromStringParts(string[] parts)
		{
			VnId = Convert.ToInt32(GetPart(parts, "id"));
			AnimeId = Convert.ToInt32(GetPart(parts, "aid"));
		}
		public int VnId { get; set; }
		public int AnimeId { get; set; }

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public AnimeItem ToAnimeItem(Dictionary<int, DumpAnime> animeDict)
		{
			var anime = animeDict[AnimeId];
			return new AnimeItem
			{
				ID = AnimeId,
				Year = anime.Year,
				RomajiTitle = anime.Title_Romaji,
				OriginalTitle = anime.Title_Kanji
			};
		}
	}

	public class DumpScreen : IDumpItem
	{
		void IDumpItem.LoadFromStringParts(string[] parts)
		{
			var id = GetPart(parts, "id");
			Id = id == "\\N" ? null : id;
			Width = Convert.ToInt32(GetPart(parts, "width"));
			Height = Convert.ToInt32(GetPart(parts, "height"));
			var sexualAvg = GetPart(parts, "c_sexual_avg");
			Sexual = sexualAvg == "\\N" ? 0 : Convert.ToDouble(sexualAvg);
			var violenceAvg = GetPart(parts, "c_violence_avg");
			Violence = violenceAvg == "\\N" ? 0 : Convert.ToDouble(violenceAvg);
		}

		public int Height { get; set; }

		public int Width { get; set; }
		
		public double Sexual { get; set; }

		public double Violence { get; set; }

		public bool Nsfw => Sexual >= 1.5 || Violence >= 1.5;

		public string Id { get; set; }

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}
	}

	public class DumpVnScreen : IDumpItem
	{
		void IDumpItem.LoadFromStringParts(string[] parts)
		{
			VnId = Convert.ToInt32(GetPart(parts, "id"));
			var imageId = GetPart(parts, "scr");
			ImageId = imageId == "\\N" ? null : imageId;
			//Nsfw = GetPart(parts, "nsfw") == "t";
		}

		//public bool Nsfw { get; set; }

		public string ImageId { get; set; }

		public int VnId { get; set; }

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public ScreenItem ToScreenItem(Dictionary<string, DumpScreen> imageDictionary)
		{
			if (!imageDictionary.TryGetValue(ImageId, out var image)) return null;
			return new ScreenItem
			{
				ImageId = ImageId,
				Nsfw = image.Nsfw,
				Height = image.Height,
				Width = image.Width
			};
		}
	}
}
