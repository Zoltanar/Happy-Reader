using System;
using System.Collections.Generic;
using Happy_Apps_Core.Database;
// ReSharper disable InconsistentNaming

namespace Happy_Apps_Core.DumpReader;

public class DumpRelation : DumpItem
{
    public override void LoadFromStringParts(string[] parts)
    {
        Id = GetInteger(parts, "id", 1);
        VnId = GetInteger(parts, "vid", 1);
        Relation = GetPartOrNull(parts, "relation");
        Official = GetBoolean(parts, "official");
    }

    public bool Official { get; set; }
    public string Relation { get; set; }
    public int VnId { get; set; }
    public int Id { get; set; }

    public RelationsItem ToRelationItem()
    {
        return new()
        {
            ID = VnId,
            Relation = Relation,
            Official = Official
        };
    }
}

public class DumpAnime : DumpItem
{
    public override void LoadFromStringParts(string[] parts)
    {
        Id = GetInteger(parts, "id");
        var yearString = GetPartOrNull(parts, "year");
        Year = yearString == null ? -1 : Convert.ToInt32(yearString);
        Title_Kanji = GetPartOrNull(parts, "title_kanji");
        Title_Romaji = GetPartOrNull(parts, "title_romaji");
    }

    public int Id { get; set; }
    public int Year { get; set; }
    public string Title_Romaji { get; set; }
    public string Title_Kanji { get; set; }
}

public class DumpVnAnime : DumpItem
{
    public override void LoadFromStringParts(string[] parts)
    {
        VnId = GetInteger(parts, "id", 1);
        AnimeId = GetInteger(parts, "aid");
    }

    public int VnId { get; set; }
    public int AnimeId { get; set; }

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

public class DumpScreen : DumpItem
{
    public override void LoadFromStringParts(string[] parts)
    {
        Id = GetPartOrNull(parts, "id");
        Width = GetInteger(parts, "width");
        Height = GetInteger(parts, "height");
        Sexual = GetDouble(parts, "c_sexual_avg");
        Violence = GetDouble(parts, "c_violence_avg");
    }

    public int Height { get; set; }
    public int Width { get; set; }
    public double Sexual { get; set; }
    public double Violence { get; set; }
    public bool Nsfw => Sexual >= 1.5 || Violence >= 1.5;
    public string Id { get; set; }
}

public class DumpVnScreen : DumpItem
{
    public override void LoadFromStringParts(string[] parts)
    {
        VnId = GetInteger(parts, "id", 1);
        ImageId = GetPartOrNull(parts, "scr");
        //Nsfw = GetPart(parts, "nsfw") == "t";
    }

    //public bool Nsfw { get; set; }
    public string ImageId { get; set; }
    public int VnId { get; set; }

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