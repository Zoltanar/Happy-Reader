using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace Happy_Apps_Core.Database;

/// <summary>
/// Contains original and other languages available for vn.
/// </summary>
[Serializable]
public class VNLanguages
{
    /// <summary>
    /// Languages for original release
    /// </summary>
    public LangRelease[] Originals { get; set; }
    /// <summary>
    /// Languages for other releases
    /// </summary>
    public LangRelease[] Others { get; set; }

    /// <summary>
    /// Languages for all releases (Originals first)
    /// </summary>
    public IEnumerable<LangRelease> All => Originals.Concat(Others);

    /// <summary>
    /// Empty Constructor for serialization
    /// </summary>
    public VNLanguages()
    {
        Originals = Array.Empty<LangRelease>();
        Others = Array.Empty<LangRelease>();
    }

    /// <summary>
    /// Constructor for vn languages.
    /// </summary>
    /// <param name="originals">Languages for original release</param>
    /// <param name="all">Languages for all releases</param>
    public VNLanguages(List<LangRelease> originals, List<LangRelease> all)
    {
        Originals = originals.ToArray();
        Others = all.Except(originals).ToArray();
    }

    /// <summary>
    /// Displays a json-serialized string.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class LangRelease : DumpItem
{
    private bool? _hasFullDate;
    private string _releaseDateString;
    public override void LoadFromStringParts(string[] parts)
    {
        ReleaseId = GetInteger(parts, "id", 1);
        Lang = GetPart(parts, "lang");
        Mtl = GetBoolean(parts, "mtl");
    }

    public string Lang { get; set; }

    /// <summary>
    /// We don't need to save this to the database
    /// </summary>
    [JsonIgnore]
    public int ReleaseId { get; set; }

    public bool Mtl { get; set; }

    public bool Partial { get; set; }

    [JsonIgnore]
    public bool HasFullDate
    {
        get
        {
            if (_hasFullDate == null)
            {
                SetReleaseDate(ReleaseDateString);
            }
            Debug.Assert(_hasFullDate != null, nameof(_hasFullDate) + " != null");
            return _hasFullDate.Value;
        }
    }

    public string ReleaseDateString
    {
        get => _releaseDateString;
        set => SetReleaseDate(value);
    }

    [JsonIgnore]
    public DateTime ReleaseDate { get; set; }

    public void SetReleaseDate(string releaseDateString)
    {
        _releaseDateString = releaseDateString;
        ReleaseDate = StaticHelpers.StringToDate(releaseDateString, out var hasFullDate);
        _hasFullDate = hasFullDate;
    }
    /// <summary>
    /// Displays a json-serialized string.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}