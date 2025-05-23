﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;

namespace Happy_Apps_Core.DumpReader;

public class DumpReader
{
    public int UserId { get; }
    public string DumpFolder { get; }
    public string OutputFilePath { get; }
    public VisualNovelDatabase Database { get; }
    public SuggestionScorer SuggestionScorer { get; }
    /// <summary>
    /// Key is release id.
    /// </summary>
    public Dictionary<int, List<LangRelease>> LangReleases { get; } = new();
    public Dictionary<int, List<Release>> VnReleases { get; } = new();
    /// <summary>
    /// Key is release id, value is first link id
    /// </summary>
    public Dictionary<int, int> ReleaseLinks { get; } = new();
    /// <summary>
    /// Key is link id, value is link
    /// </summary>
    public Dictionary<int, string> ExternalLinks { get; } = new();
    public Dictionary<int, DumpAnime> Animes { get; } = new();
    public Dictionary<int, List<DumpVnAnime>> VnAnimes { get; } = new();
    public Dictionary<string, DumpScreen> Images { get; } = new();
    public Dictionary<int, List<DumpVnScreen>> VnScreens { get; } = new();
    public Dictionary<int, List<DumpRelation>> VnRelations { get; } = new();
    public Dictionary<int, UserVN.LabelKind> UserLabels { get; } = new();
    public Dictionary<int, UserVn> UserVns { get; } = new();
    public Dictionary<int, List<DumpTitle>> VnTitles { get; } = new();
    private Dictionary<int, List<LengthVote>> VnLengths { get; } = new();
    public List<VnTag> VnTags { get; } = new();
    public Dictionary<int, List<DumpVote>> Votes { get; } = new();

    public DumpReader(string dumpFolder, string currentDatabaseFilePath, int userId, out string inProgressDbFile)
    {
        if (!Directory.Exists(dumpFolder)) throw new IOException($"Dump folder does not exist: '{dumpFolder}'");
        var currentDbFile = new FileInfo(currentDatabaseFilePath);
        if (!currentDbFile.Exists) throw new IOException($"Original database does not exist: '{currentDbFile}'");
        //UIP = update in progress
        inProgressDbFile = GetDatabaseFile(currentDbFile, "-UIP");
        File.Copy(currentDatabaseFilePath, inProgressDbFile);
        //DRB = dump reader backup
        var backupPath = GetDatabaseFile(currentDbFile, "-DRB");
        DumpReaderStarter.PrintLogLine([$"Backing up database to {backupPath}"]);
        File.Copy(currentDbFile.FullName, backupPath);
        UserId = userId;
        DumpFolder = dumpFolder;
        OutputFilePath = inProgressDbFile;
        Database = new VisualNovelDatabase(OutputFilePath, false);
        Database.DeleteForDump();
        DumpFiles.Load();
        SuggestionScorer = new SuggestionScorer(
            StaticHelpers.CSettings.GetTagScoreDictionary(),
            StaticHelpers.CSettings.GetTraitScoreDictionary(),
            Database);
    }

    private string GetDatabaseFile(FileInfo currentDatabaseFile, string suffix)
    {
        var dbDirectory = Path.GetDirectoryName(currentDatabaseFile.FullName) ?? throw new ArgumentNullException(nameof(Path.GetDirectoryName));
        return Path.Combine(dbDirectory, $"{Path.GetFileNameWithoutExtension(currentDatabaseFile.FullName)}{suffix}{DateTime.Now:yyyyMMdd-HHmmss}{currentDatabaseFile.Extension}");
    }
    public void Run(DateTime dumpDate, int[] previousVnIds, int[] previousCharacterIds)
    {
        DumpReaderStarter.PrintLogLine(["Starting Dump Reader..."]);
        var votesFilePath = FindVotesFile();
        Load<ListedProducer>((i, t) => Database.Producers.Add(i, false, true, t), "db\\producers");
        LoadLinks();
        LoadReleases();
        LoadAndResolveTags();
        LoadStaff();
        LoadCharacters(previousCharacterIds);
        Load<DumpVote>((vote, _) =>
        {
            if (!Votes.TryGetValue(vote.VNId, out var listOfVotes)) listOfVotes = Votes[vote.VNId] = new List<DumpVote>();
            listOfVotes.Add(vote);
        }, votesFilePath, false);
        LoadAnimeScreensRelations();
        LoadUserVn();
        Load<DumpTitle>((i, _) =>
        {
            if (!VnTitles.ContainsKey(i.VNId)) VnTitles[i.VNId] = new List<DumpTitle>();
            VnTitles[i.VNId].Add(i);
        }, "db\\vn_titles");
        Load<LengthVote>((i, _) =>
        {
            if (!VnLengths.ContainsKey(i.VNId)) VnLengths[i.VNId] = new List<LengthVote>();
            if (i.ReleaseIds.Any()) VnLengths[i.VNId].Add(i);
        }, "db\\vn_length_votes");
        var newTitleCount = 0;
        Load<ListedVN>((i, t) =>
        {
            ResolveOtherForVn(i);
            //if database was not empty and this vn wasn't in previous update
            if (previousVnIds.Length != 0 && Array.BinarySearch(previousVnIds, i.VNID) < 0)
            {
                i.NewSinceUpdate = true;
                newTitleCount++;
            }
            Database.VisualNovels.Add(i, false, true, t);
            ResolveUserVnForVn(i, t);
        }, "db\\vn");
        DumpReaderStarter.PrintLogLine([$"Added {newTitleCount} new titles."]);
        Database.SaveLatestDumpUpdate(dumpDate);
        DumpReaderStarter.PrintLogLine(["Completed."]);
    }

    private void LoadLinks()
    {
        DumpReaderStarter.PrintLogLine(["Loading Links..."]);
        using var externalLinksFile = new StreamReader(File.Open(Path.Combine(DumpFolder, "db\\extlinks"), FileMode.Open));
        while (externalLinksFile.ReadLine() is string line)
        {
            Debug.Assert(line != null, nameof(line) + " != null");
            var parts = line.Split('\t');
            if (!parts[1].Equals("website")) continue;
            ExternalLinks[int.Parse(parts[0])] = parts[2];
        }
        using var releaseLinksFile = new StreamReader(File.Open(Path.Combine(DumpFolder, "db\\releases_extlinks"), FileMode.Open));
        while (releaseLinksFile.ReadLine() is string line)
        {
            Debug.Assert(line != null, nameof(line) + " != null");
            var parts = line.Split('\t');
            var releaseId = int.Parse(parts[0].Substring(1));
            if (ReleaseLinks.ContainsKey(releaseId)) continue;
            var linkId = int.Parse(parts[1]);
            if (!ExternalLinks.ContainsKey(linkId)) continue;
            ReleaseLinks[releaseId] = linkId;
        }
    }

    private void LoadStaff()
    {
        Load<StaffItem>((i, t) =>
        {
            Database.StaffItems.Add(i, false, true, t);
        }, "db\\staff");
        Load<StaffAlias>((i, t) =>
        {
            Database.StaffAliases.Add(i, false, true, t);
        }, "db\\staff_alias");
        Load<VnStaff>((i, t) =>
        {
            //todo vnstaff editions
            if (Database.VnStaffs[i.Key] != null) return;
            Database.VnStaffs.Add(i, false, true, t);
        }, "db\\vn_staff");
        Load<VnSeiyuu>((i, t) =>
        {
            Database.VnSeiyuus.Add(i, false, true, t);
        }, "db\\vn_seiyuu");
    }

    private void LoadAnimeScreensRelations()
    {
        Load<DumpAnime>((i, _) => Animes.Add(i.Id, i), "db\\anime");
        Load<DumpVnAnime>((i, _) =>
        {
            if (!VnAnimes.ContainsKey(i.VnId)) VnAnimes[i.VnId] = new List<DumpVnAnime>();
            VnAnimes[i.VnId].Add(i);
        }, "db\\vn_anime");
        Load<DumpScreen>((i, _) => Images.Add(i.Id, i), "db\\images");
        Load<DumpVnScreen>((i, _) =>
        {
            if (!VnScreens.ContainsKey(i.VnId)) VnScreens[i.VnId] = new List<DumpVnScreen>();
            VnScreens[i.VnId].Add(i);
        }, "db\\vn_screenshots");
        Load<DumpRelation>((i, _) =>
        {
            if (!VnRelations.ContainsKey(i.Id)) VnRelations[i.Id] = new List<DumpRelation>();
            VnRelations[i.Id].Add(i);
        }, "db\\vn_relations");
    }

    private void LoadCharacters(int[] previousCharacterIds)
    {
        Load<CharacterVN>((i, t) =>
        {
            if (Database.CharacterVNs.ByKey(i.ListKey, i.Key) != null) return;
            Database.CharacterVNs.Add(i, false, true, t);
        }, "db\\chars_vns");
        Load<DbTrait>((i, t) =>
        {
            Database.Traits.Add(i, false, true, t);
        }, "db\\chars_traits");
        Load<CharacterItem>((i, t) =>
        {
            SuggestionScorer.SetScore(i, Database.Traits[i.ID].Select(trait => trait.TraitId));
            if (previousCharacterIds.Length != 0 && Array.BinarySearch(previousCharacterIds, i.ID) < 0) i.NewSinceUpdate = true;
            Database.Characters.Add(i, false, true, t);
        }, "db\\chars");
    }

    private void LoadReleases()
    {
        Dictionary<int, List<int>> producerReleases = new();
        Dictionary<int, Release> releases = new();
        Load<ProducerRelease>((i, _) =>
            {
                if (!i.Developer) return;
                if (!producerReleases.ContainsKey(i.ReleaseId)) producerReleases[i.ReleaseId] = new List<int>();
                producerReleases[i.ReleaseId].Add(i.ProducerId);
            }, "db\\releases_producers");
        Load<LangRelease>((i, _) =>
        {
            if (!LangReleases.ContainsKey(i.ReleaseId)) LangReleases[i.ReleaseId] = new List<LangRelease>();
            LangReleases[i.ReleaseId].Add(i);
        }, "db\\releases_titles");
        Load<Release>((i, _) => releases[i.ReleaseId] = i, "db\\releases");
        Load<VnRelease>((i, _) =>
        {
            if (!VnReleases.ContainsKey(i.VnId)) VnReleases[i.VnId] = new List<Release>();
            if (!releases.TryGetValue(i.ReleaseId, out var release)) return;
            if (i.ReleaseType == "trial")
            {
                releases.Remove(i.ReleaseId);
                return;
            }
            release.Languages = LangReleases[i.ReleaseId];
            foreach (var langRelease in release.Languages) langRelease.Partial = i.ReleaseType == "partial";
            producerReleases.TryGetValue(i.ReleaseId, out var producerRelease);
            release.Producers = producerRelease ?? new List<int>();
            VnReleases[i.VnId].Add(release);
        }, "db\\releases_vn");
        Load<ReleaseImage>((i, _) =>
        {
            if (!releases.TryGetValue(i.ReleaseId, out var release)) return;
            release.Images.Add(i);
        }, "db\\releases_images");
        //clear collections to save memory
        producerReleases.Clear();
        releases.Clear();
        GC.Collect();
    }

    private void ResolveUserVnForVn(ListedVN vn, SQLiteTransaction transaction)
    {
        if (!UserVns.TryGetValue(vn.VNID, out var dumpUserVn)) return;
        var labels = dumpUserVn.LabelsString.Substring(1, dumpUserVn.LabelsString.Length - 2).Split(',').Select(i => UserLabels[int.Parse(i)]).ToHashSet();
        var userVn = new UserVN
        {
            UserId = UserId,
            VNID = vn.VNID,
            ULNote = dumpUserVn.Notes,
            Added = dumpUserVn.Added,
            LastModified = dumpUserVn.LastModified,
            Labels = labels,
            Started = dumpUserVn.Started,
            Finished = dumpUserVn.Finished
        };
        if (Votes.TryGetValue(vn.VNID, out var dumpvVote))
        {
            var vote = dumpvVote.FirstOrDefault(v => v.UserId == UserId);
            if (vote != null)
            {
                userVn.Vote = vote.Vote;
                userVn.VoteAdded = vote.Date;
            }
        }
        Database.UserVisualNovels.Add(userVn, false, true, transaction);
    }

    private void LoadUserVn()
    {
        Load<UserLabel>((i, _) =>
        {
            if (i.UserId != UserId) return;
            UserLabels.Add(i.LabelId, i.Label);
        }, "db\\ulist_labels");
        Load<UserVn>((i, _) =>
        {
            if (i.UserId != UserId) return;
            UserVns.Add(i.VnId, i);
        }, "db\\ulist_vns");
    }

    private string FindVotesFile()
    {
        var file = new DirectoryInfo(DumpFolder).EnumerateFiles("vndb-votes-*", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (file == null) throw new ArgumentNullException(nameof(file), "Votes file was not found.");
        return file.FullName;
    }

    private void LoadAndResolveTags()
    {
        Load<VnTag>((i, _) =>
        {
            if (i.Ignore) return;
            VnTags.Add(i);
        }, "db\\tags_vn");
        DumpReaderStarter.PrintLogLine(["Resolving Tags..."]);
        var groupedTags = VnTags.GroupBy(t => (t.TagId, t.VnId)).ToArray();
        WrapInTransaction(trans =>
        {
            foreach (var group in groupedTags)
            {
                var spoilerTags = group.Where(t => t.Spoiler.HasValue).ToList();
                var dbTag = new DbTag
                {
                    TagId = group.Key.TagId,
                    VNID = group.Key.VnId,
                    Score = group.Average(t => t.Vote),
                    // ReSharper disable once PossibleInvalidOperationException
                    Spoiler = spoilerTags.Any() ? (int)Math.Round(spoilerTags.Average(t => t.Spoiler.Value)) : 0
                };
                dbTag.SetCategory();
                Database.Tags.Add(dbTag, false, true, trans);
            }
        });
    }

    private void Load<T>(Action<T, SQLiteTransaction> addToList, string filePath, bool useHeaderFile = true) where T : DumpItem, new()
    {
        DumpReaderStarter.PrintLogLine([$"Loading for {typeof(T).Name}..."]);
        new T().SetDumpHeaders((useHeaderFile
            ? File.ReadAllLines(Path.Combine(DumpFolder, filePath + ".header")).Single()
            : string.Empty).Split('\t'));
        WrapInTransaction(trans =>
        {
            using var file = new StreamReader(File.Open(Path.Combine(DumpFolder, filePath), FileMode.Open));
            while (file.ReadLine() is string line)
            {
                Debug.Assert(line != null, nameof(line) + " != null");
                var parts = line.Split('\t');
                var item = new T();
                item.LoadFromStringParts(parts);
                addToList(item, trans);
            }
        });
    }

    private void WrapInTransaction(Action<SQLiteTransaction> action, [CallerMemberName] string caller = null)
    {
        Database.Connection.Open();
        SQLiteTransaction transaction = null;
        try
        {
            transaction = Database.Connection.BeginTransaction();
            action(transaction);
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction?.Rollback();
            StaticHelpers.Logger.ToFile(ex, caller);
            throw;
        }
        finally
        {
            Database.Connection.Close();
        }
    }

    private void ResolveOtherForVn(ListedVN vn)
    {
        if (Votes.ContainsKey(vn.VNID))
        {
            var votes = Votes[vn.VNID];
            vn.VoteCount = votes.Count;
            if (votes.Count > 0) vn.Rating = votes.Average(v => v.Vote);
        }

        if ((vn.LengthTime?.Equals(LengthFilterEnum.NA) ?? true) && VnLengths.TryGetValue(vn.VNID, out var lengths)) ResolveLength(vn, lengths);
        ResolveRelease();
        ResolveTitle();
        ResolveRelations();
        ResolveAnime();
        ResolveScreens();
        SuggestionScorer.SetScore(vn, false, Database);

        void ResolveScreens()
        {
            if (!string.IsNullOrWhiteSpace(vn.ImageId) && Images.TryGetValue(vn.ImageId, out var cover)) vn.ImageNSFW = cover.Nsfw;
            var screensObject = VnScreens.TryGetValue(vn.VNID, out var screens)
                ? screens.Select(r => r.ToScreenItem(Images)).Where(i => i != null).ToArray()
                : Array.Empty<ScreenItem>();
            vn.SetScreens(JsonConvert.SerializeObject(screensObject), screensObject);
        }
        void ResolveAnime()
        {
            var animeObject = VnAnimes.TryGetValue(vn.VNID, out var animes)
                ? animes.Select(r => r.ToAnimeItem(Animes)).ToArray()
                : Array.Empty<AnimeItem>();
            vn.SetAnime(JsonConvert.SerializeObject(animeObject), animeObject);
        }
        void ResolveRelations()
        {
            var relationsObject = VnRelations.TryGetValue(vn.VNID, out var relations)
                ? relations.Select(r => r.ToRelationItem()).ToArray()
                : Array.Empty<RelationsItem>();
            vn.SetRelations(JsonConvert.SerializeObject(relationsObject), relationsObject);
        }
        void ResolveRelease()
        {
            if (!VnReleases.ContainsKey(vn.VNID)) return;
            var releases = VnReleases[vn.VNID].OrderBy(r => r.Released).ToList();
            if (!releases.Any()) return;
            //set release date to each LangRelease object
            foreach (var rel in releases)
            {
                foreach (var lang in rel.Languages)
                {
                    lang.SetReleaseDate(StringToDateString(rel.Released));
                }
            }
            var otherLanguages = FilterLanguages(releases, out var firstRelease);
            vn.SetReleaseDate(StringToDateString(firstRelease.Released));
            var languages = new VNLanguages(firstRelease.Languages, otherLanguages);
            vn.Languages = JsonConvert.SerializeObject(languages);
            vn.ProducerID = releases.SelectMany(r => r.Producers).FirstOrDefault(p => Database.Producers[p] != null);
            if (ReleaseLinks.TryGetValue(firstRelease.ReleaseId, out var linkId) && ExternalLinks.TryGetValue(linkId, out var link)) vn.ReleaseLink = link;
            ResolveVnImage(vn, releases);
        }
        void ResolveTitle()
        {
            if (VnTitles.TryGetValue(vn.VNID, out var titles))
            {
                var cTitles = titles.Where(t => t.Lang == vn.OriginalLanguage).OrderByDescending(t => t.Official).ToList();
                var chosen = cTitles.First();
                vn.Title = string.IsNullOrWhiteSpace(chosen.Latin) ? chosen.Title : chosen.Latin;
                vn.KanjiTitle = string.IsNullOrWhiteSpace(chosen.Latin) ? null : chosen.Title;
            }
            else vn.Title = "(No title found)";
        }
    }

    private void ResolveVnImage(ListedVN vn, List<Release> releases)
    {
        if(vn.ImageId != null ) return;
        var image = releases.SelectMany(r => r.Images).OrderBy(image =>
        {
            return image.Type switch
            {
                "pkgfront" => 0,
                "dig" => 1,
                "pkgcontent" => 2,
                "pkgmed" => 3,
                "pkgback" => 4,
                "pkgside" => 4,
                _ => -1,
            };
        }).ThenBy(image =>
        {
            foreach (var language in image.Languages ?? Array.Empty<string>())
            {
                if (language == "en") return 0;
                if (language == StaticHelpers.CSettings.SecondaryTitleLanguage) return 1;
            }
            return 2;
        }).FirstOrDefault();
        if (image != null) vn.ImageId = $"cv{image.Image}";
    }

    private void ResolveLength(ListedVN vn, List<LengthVote> lengthVotes)
    {
        var completeReleaseLengths = new List<int>();
        var partialReleaseLengths = new List<int>();
        foreach (var lengthVote in lengthVotes)
        {
            var complete = false;
            foreach (var release in lengthVote.ReleaseIds)
            {
                if (!LangReleases.TryGetValue(release, out var langReleases)) continue;
                if (langReleases.Any(r => !r.Partial)) complete = true;
            }
            (complete ? completeReleaseLengths : partialReleaseLengths).Add(lengthVote.Length);
        }
        var lengths = completeReleaseLengths.Any() ? completeReleaseLengths : partialReleaseLengths;
        if (!lengths.Any()) return;
        var lengthHours = lengths.Average() / 60d;
        vn.LengthTime = lengthHours switch
        {
            < 2 => LengthFilterEnum.UnderTwoHours,
            < 10 => LengthFilterEnum.TwoToTenHours,
            < 30 => LengthFilterEnum.TenToThirtyHours,
            < 50 => LengthFilterEnum.ThirtyToFiftyHours,
            _ => LengthFilterEnum.OverFiftyHours
        };
    }

    private static string StringToDateString(string released)
    {
        if (string.IsNullOrWhiteSpace(released)) return "";
        if (released.StartsWith("0000")) { }
        var day = released.Substring(6, 2);
        var month = released.Substring(4, 2);
        var year = released.Substring(0, 4);
        if (year == "9999") return "";
        if (month == "99") return year;
        return day == "99" ? $"{year}-{month}" : $"{year}-{month}-{day}";
    }

    private static List<LangRelease> FilterLanguages(List<Release> releases, out Release firstRelease)
    {
        firstRelease = releases.First();
        var firstLanguages = firstRelease.Languages.Select(l => l.Lang).ToList();
        var languages = releases.Skip(1).SelectMany(l => l.Languages).Where(lang => !firstLanguages.Contains(lang.Lang));
        //not machine translation first, not partial first
        var filteredLanguages = languages.GroupBy(l => l.Lang).Select(g => g
            .OrderBy(l2 => l2.Mtl)
            .ThenBy(l2 => l2.Partial)
            .First()
        ).ToList();
        return filteredLanguages;
    }

    public static void GetDbStats(string databaseFile, out DateTime? latestDumpUpdate, out int[] vnIds, out int[] characterIds)
    {
        if (!File.Exists(databaseFile))
        {
            latestDumpUpdate = null;
            vnIds = Array.Empty<int>();
            characterIds = Array.Empty<int>();
            return;
        }
        var database = new VisualNovelDatabase(databaseFile, false);
        latestDumpUpdate = database.GetLatestDumpUpdate();
        database.VisualNovels.Load(true);
        database.Characters.Load(true);
        //we order this collection so we can run a binary search on it
        vnIds = database.VisualNovels.Select(v => v.VNID).OrderBy(n => n).ToArray();
        characterIds = database.Characters.Select(v => v.ID).OrderBy(n => n).ToArray();
    }
}