using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

namespace Happy_Apps_Core
{
    /// <summary>
    /// Object for displaying Visual Novel in Object List View.
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ListedVN : INotifyPropertyChanged
    {
        /// <summary>
        /// Returns true if a title was last updated over x days ago.
        /// </summary>
        /// <param name="days">Days since last update</param>
        /// <param name="fullyUpdated">Use days since full update</param>
        /// <returns></returns>
        public bool LastUpdatedOverDaysAgo(int days, bool fullyUpdated = false)
        {
            var dateToUse = fullyUpdated ? DaysSinceFullyUpdated : UpdatedDate;
            if (dateToUse == -1) return true;
            return dateToUse > days;
        }

        #region Columns
        /// <summary>
        /// VN's ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int VNID { get; set; }

        /// <summary>
        /// VN title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// VN kanji title
        /// </summary>
        public string KanjiTitle { get; set; }

        /// <summary>
        /// VN's first non-trial release date, set by calling SetReleaseDate(string)
        /// </summary>
        public string ReleaseDateString { get; set; }

        public int? ProducerID { get; set; }

        public virtual ListedProducer Producer { get; set; }

        public string Tags { get; set; }

        public DateTime DateUpdated { get; set; }

        /// <summary>
        /// URL of VN's cover image
        /// </summary>
        public string ImageURL { get; set; }

        /// <summary>
        /// Is VN's cover NSFW?
        /// </summary>
        public bool ImageNSFW { get; set; }

        /// <summary>
        /// VN description
        /// </summary>
        public string Description { get; set; }

        public LengthFilter? LengthTime { get; set; }

        /// <summary>
        /// Popularity of VN, percentage of most popular VN
        /// </summary>
        public double Popularity { get; set; }

        /// <summary>
        /// Bayesian rating of VN, 1-10
        /// </summary>
        public double Rating { get; set; }

        /// <summary>
        /// Number of votes cast on VN
        /// </summary>
        public int VoteCount { get; set; }

        /// <summary>
        /// JSON Array string containing List of Relation Items
        /// </summary>
        public string Relations { get; set; }

        /// <summary>
        /// JSON Array string containing List of Screenshot Items
        /// </summary>
        public string Screens { get; set; }

        /// <summary>
        /// JSON Array string containing List of Anime Items
        /// </summary>
        public string Anime { get; set; }

        /// <summary>
        /// Newline separated string of aliases
        /// </summary>
        public string Aliases { get; set; }

        public string Languages { get; set; }

        public DateTime DateFullyUpdated { get; set; }

        public int? UserVNId { get; set; }

        public virtual UserVN UserVN
        { get; set; }

        public DateTime ReleaseDate { get; set; }
        #endregion

        public void SetReleaseDate(string releaseDateString)
        {
            ReleaseDateString = releaseDateString;
            ReleaseDate = StringToDate(releaseDateString);
        }

        private int? _daysSinceFullyUpdated;
        /// <summary>
        /// Days since all fields were updated
        /// </summary>
        public int DaysSinceFullyUpdated
        {
            get
            {
                if (_daysSinceFullyUpdated == null) _daysSinceFullyUpdated = (int)(DateTime.UtcNow - DateFullyUpdated).TotalDays;
                return _daysSinceFullyUpdated.Value;
            }
        }

        /// <summary>
        /// List of Tags in this VN.
        /// </summary>
        [NotMapped, NotNull]
        public VNItem.TagItem[] TagList
        {
            get
            {
                if (_tagList == null)
                {
                    _tagList = StringToTags(Tags);
                    foreach (var tag in _tagList) tag.SetCategory(DumpFiles.PlainTags);
                }
                return _tagList;
            }
        }

        [NotMapped, NotNull]
        public IEnumerable<string> DisplayTags
        {
            get
            {
                var visibleTags = new List<VNItem.TagItem>();
                foreach (var tag in TagList)
                {
                    switch (tag.Category)
                    {
                        case TagCategory.Content:
                            if (!GSettings.ContentTags) continue;
                            break;
                        case TagCategory.Sexual:
                            if (!GSettings.SexualTags) continue;
                            break;
                        case TagCategory.Technical:
                            if (!GSettings.TechnicalTags) continue;
                            break;
                    }
                    visibleTags.Add(tag);
                }
                if (visibleTags.Count == 0) return new[] { "No Tags Found" };
                List<string> stringList = visibleTags.Select(x => x.Print(DumpFiles.PlainTags)).ToList();
                stringList.Sort();
                return stringList;
            }
        }

        [NotMapped, NotNull]
        public IEnumerable<string> DisplayTraits
        {
            get
            {
                var vnCharacters = GetCharacters(LocalDatabase.Characters);
                var stringList = new List<string> { $"{vnCharacters.Length} Characters" };
                for (var index = 0; index < vnCharacters.Length; index++)
                {
                    var characterItem = vnCharacters[index];
                    stringList.Add($"Character {characterItem.ID}");
                    foreach (var trait in characterItem.Traits) stringList.Add(DumpFiles.PlainTraits.Find(x => x.ID == trait.ID)?.ToString());
                    if (index < vnCharacters.Length - 1) stringList.Add("---------------");
                }
                return stringList;
            }
        }

        [NotMapped, NotNull]
        public IEnumerable<string> DisplayRelations
        {
            get
            {
                if (!RelationsObject.Any()) return new List<string> { "No relations found." };
                var titleString = RelationsObject.Length == 1 ? "1 Relation" : $"{RelationsObject.Length} Relations";
                var stringList = new List<string> { titleString, "--------------" };
                IGrouping<string, VNItem.RelationsItem>[] groups = RelationsObject.GroupBy(x => x.Relation).ToArray();
                for (var index = 0; index < groups.Length; index++)
                {
                    IGrouping<string, VNItem.RelationsItem> group = groups[index];
                    stringList.AddRange(group.Select(relation => relation.Print()));
                    if (index < groups.Length - 1) stringList.Add("---------------");
                }
                return stringList;
            }
        }

        [NotMapped, NotNull]
        public IEnumerable<string> DisplayAnime
        {
            get
            {
                if (!AnimeObject.Any()) return new List<string> { "No anime found." };
                var titleString = $"{AnimeObject.Length} Anime";
                var stringList = new List<string> { titleString, "--------------" };
                stringList.AddRange(AnimeObject.Select(x => x.Print()));
                return stringList;
            }
        }

        [NotMapped, NotNull]
        public VNItem.RelationsItem[] RelationsObject
        {
            get
            {
                if (_relationsObject != null) return _relationsObject;
                if (string.IsNullOrWhiteSpace(Relations)) _relationsObject = new VNItem.RelationsItem[] { };
                else _relationsObject = JsonConvert.DeserializeObject<VNItem.RelationsItem[]>(Relations) ?? new VNItem.RelationsItem[] { };
                return _relationsObject;
            }
        }

        [NotMapped, NotNull]
        public VNItem.AnimeItem[] AnimeObject
        {
            get
            {
                if (_animeObject != null) return _animeObject;
                if (string.IsNullOrWhiteSpace(Anime)) _animeObject = new VNItem.AnimeItem[] { };
                else _animeObject = JsonConvert.DeserializeObject<VNItem.AnimeItem[]>(Anime) ?? new VNItem.AnimeItem[] { };
                return _animeObject;
            }
        }

        /// <summary>
        /// Gets characters involved in VN.
        /// </summary>
        /// <param name="characterList">List of all characters</param>
        /// <returns>Array of Characters</returns>
        public CharacterItem[] GetCharacters(IEnumerable<CharacterItem> characterList)
        {
            return characterList.Where(x => x.CharacterIsInVN(VNID)).ToArray();
        }

        private VNItem.RelationsItem[] _relationsObject;
        private VNItem.AnimeItem[] _animeObject;
        private VNItem.TagItem[] _tagList;
        private VNLanguages _languagesObject;

        /// <summary>
        /// Days since last tags/stats/traits update
        /// </summary>
        [NotMapped]
        public int UpdatedDate { get; set; }

        /// <summary>
        /// Language of producer
        /// </summary>
        [NotMapped, NotNull]
        public VNLanguages LanguagesObject => _languagesObject ?? (_languagesObject = JsonConvert.DeserializeObject<VNLanguages>(Languages) ?? new VNLanguages());

        /// <summary>
        /// Return unreleased status of vn.
        /// </summary>
        public UnreleasedFilter Unreleased
        {
            get
            {
                if (ReleaseDate == DateTime.MaxValue) return UnreleasedFilter.WithoutReleaseDate;
                return ReleaseDate > DateTime.Today ? UnreleasedFilter.WithReleaseDate : UnreleasedFilter.Released;
            }
        }

        /// <summary>
        /// Gets blacklisted status of vn.
        /// </summary>
        public bool Blacklisted => UserVN.WLStatus == WishlistStatus.Blacklist;

        /// <summary>
        /// Gets voted status of vn.
        /// </summary>
        public bool Voted => UserVN.Vote >= 1;

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => $"[{VNID}] {Title}";

        /// <summary>
        /// Get VN's User-related status as a string.
        /// </summary>
        /// <returns>User-related status</returns>
        public string UserRelatedStatus
        {
            get
            {
                string[] parts = { "", "", "" };
                if (UserVN?.ULStatus > UserlistStatus.None)
                {
                    parts[0] = "Userlist: ";
                    parts[1] = UserVN.ULStatus.ToString();
                }
                else if (UserVN?.WLStatus > WishlistStatus.None)
                {
                    parts[0] = "Wishlist: ";
                    parts[1] = UserVN.WLStatus.ToString();
                }
                if (UserVN?.Vote > 0) parts[2] = $" (Vote: {UserVN.Vote:0.#})";
                return string.Join(" ", parts);
            }
        }

        /// <summary>
        /// Checks if title was released between two dates, the recent date is inclusive.
        /// Make sure to enter arguments in correct order.
        /// </summary>
        /// <param name="oldDate">Date furthest from the present</param>
        /// <param name="recentDate">Date closest to the present</param>
        /// <returns></returns>
        public bool ReleasedBetween(DateTime oldDate, DateTime recentDate)
        {
            return ReleaseDate > oldDate && ReleaseDate <= recentDate;
        }

        /// <summary>
        /// Checks if VN is in specified user-defined group.
        /// </summary>
        /// <param name="groupName">User-defined name of group</param>
        /// <returns>Whether VN is in the specified group</returns>
        public bool IsInGroup(string groupName)
        {
            var itemNotes = GetCustomItemNotes();
            return itemNotes.Groups.Contains(groupName);
        }

        /// <summary>
        /// Get CustomItemNotes containing note and list of groups that vn is in.
        /// </summary>
        public VNItem.CustomItemNotes GetCustomItemNotes()
        {
            //
            if (UserVN.ULNote.Equals("")) return new VNItem.CustomItemNotes("", new List<string>());
            if (!UserVN.ULNote.StartsWith("Notes: "))
            {
                //escape ulnote
                string fixedNote = UserVN.ULNote.Replace("|", "(sep)");
                fixedNote = fixedNote.Replace("Groups: ", "groups: ");
                return new VNItem.CustomItemNotes(fixedNote, new List<string>());
            }
            int endOfNotes = UserVN.ULNote.IndexOf("|", StringComparison.InvariantCulture);
            string notes;
            string groupsString;
            try
            {
                notes = UserVN.ULNote.Substring(7, endOfNotes - 7);
                groupsString = UserVN.ULNote.Substring(endOfNotes + 1 + 8);
            }
            catch (ArgumentOutOfRangeException)
            {
                notes = "";
                groupsString = "";
            }
            List<string> groups = groupsString.Equals("") ? new List<string>() : groupsString.Split(',').ToList();
            return new VNItem.CustomItemNotes(notes, groups);
        }

        /// <summary>
        /// Check if title was released in specified year.
        /// </summary>
        /// <param name="year">Year of release</param>
        public bool ReleasedInYear(int year)
        {
            return ReleaseDate.Year == year;
        }

        /// <summary>
        /// Get location of cover image in system (not online)
        /// </summary>
        public string StoredCover => $"{VNImagesFolder}{VNID}{Path.GetExtension(ImageURL)}";

        public string Series { get; set; }

        public object FlagSource => LanguagesObject.Originals.Select(language => $"{FlagsFolder}{language}.png")
                                    .Where(File.Exists).Select(Path.GetFullPath).FirstOrDefault() ?? DependencyProperty.UnsetValue;

        public Uri CoverSource
        {
            get
            {
                if (VNID == 0) return new Uri(Path.GetFullPath(NoImageFile));
                string image;
                if (ImageNSFW && !GSettings.NSFWImages) image = NsfwImageFile;
                else if (File.Exists(StoredCover)) image = StoredCover;
                else image = NoImageFile;
                return new Uri(Path.GetFullPath(image));
            }
        }

        public Brush BackBrush => GetBrushFromStatuses();

        public Brush ProducerBrush => VNIsByFavoriteProducer(this) ? FavoriteProducerBrush : Brushes.Black;

        public Brush DateBrush => ReleaseDate > DateTime.UtcNow ? UnreleasedBrush : Brushes.Black;

        public Brush UserRelatedBrush => UserVN?.ULStatus == UserlistStatus.Playing ? ULPlayingBrush : Brushes.Black;



        /// <summary>
        /// Get brush from vn UL or WL status or null if no statuses are found.
        /// </summary>
        [NotNull]
        public Brush GetBrushFromStatuses()
        {
            var brush = DefaultTileBrush;
            var success = GetColorFromULStatus(ref brush);
            if (!success) GetColorFromWLStatus(ref brush);
            return brush;
        }

        /// <summary>
        /// Return color based on wishlist status, or null if no status
        /// </summary>
        public bool GetColorFromWLStatus(ref Brush color)
        {
            if (UserVN?.WLStatus == null) return false;
            switch (UserVN.WLStatus)
            {
                case WishlistStatus.High:
                    color = WLHighBrush;
                    return true;
                case WishlistStatus.Medium:
                    color = WLMediumBrush;
                    return true;
                case WishlistStatus.Low:
                    color = WLLowBrush;
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return color based on userlist status, or null if no status
        /// </summary>
        public bool GetColorFromULStatus(ref Brush color)
        {
            if (UserVN?.ULStatus == null) return false;
            switch (UserVN?.ULStatus)
            {
                case UserlistStatus.Finished:
                    color = ULFinishedBrush;
                    return true;
                case UserlistStatus.Stalled:
                    color = ULStalledBrush;
                    return true;
                case UserlistStatus.Dropped:
                    color = ULDroppedBrush;
                    return true;
                case UserlistStatus.Unknown:
                    color = ULUnknownBrush;
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns whether vn is by a favorite producer.
        /// </summary>
        /// <param name="favoriteProducers">List of favorite producers.</param>
        public bool ByFavoriteProducer(IEnumerable<ListedProducer> favoriteProducers)
        {
            return favoriteProducers.FirstOrDefault(fp => fp.ID == ProducerID) != null;
        }

        public bool HasLanguage(string value)
        {
            return LanguagesObject.All.Contains(value);
        }

        public bool HasOriginalLanguage(string value)
        {
            return LanguagesObject.Originals.Contains(value);
        }

        public bool MatchesSingleTag(int id)
        {
            var allIds = DumpFiles.GetAllSubTags(id);
            return TagList.Any(tag => allIds.Contains(tag.ID));
        }
        public bool MatchesSingleTrait(int id)
        {
            var allIds = DumpFiles.GetAllSubTraits(id);
            return GetCharacters(LocalDatabase.Characters).Any(c => c.Traits.Any(t => allIds.Contains(t.ID)));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task GetRelationsAnimeScreens()
        {
            if (string.IsNullOrWhiteSpace(Relations))
            {
                await Conn.GetAndSetRelationsForVN(this);
                OnPropertyChanged(nameof(DisplayRelations));
            }
            if (string.IsNullOrWhiteSpace(Anime))
            {
                await Conn.GetAndSetAnimeForVN(this);
                OnPropertyChanged(nameof(DisplayAnime));
            }
            //todo
        }

        public void SetRelations(string relationsString, VNItem.RelationsItem[] relationsObject)
        {
            Relations = relationsString;
            _relationsObject = relationsObject;
        }

        public void SetAnime(string animeString, VNItem.AnimeItem[] animeObject)
        {
            Anime = animeString;
            _animeObject = animeObject;
        }
    }

    /// <summary>
    /// Contains original and other languages available for vn.
    /// </summary>
    [Serializable]
    public class VNLanguages
    {
        /// <summary>
        /// Languages for original release
        /// </summary>
        public string[] Originals { get; set; }
        /// <summary>
        /// Languages for other releases
        /// </summary>
        public string[] Others { get; set; }

        /// <summary>
        /// Languages for all releases
        /// </summary>
        public IEnumerable<string> All => Originals.Concat(Others);

        /// <summary>
        /// Empty Constructor for serialization
        /// </summary>
        public VNLanguages() { }

        /// <summary>
        /// Constructor for vn languages.
        /// </summary>
        /// <param name="originals">Languages for original release</param>
        /// <param name="all">Languages for all releases</param>
        public VNLanguages(string[] originals, string[] all)
        {
            Originals = originals;
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

    public enum UnreleasedFilter : long
    {
        [Description("Unreleased without date")]
        WithoutReleaseDate = 1,
        [Description("Unreleased with date")]
        WithReleaseDate = 2,
        [Description("Released")]
        Released = 3
    }

    public enum LengthFilter : long
    {
        [Description("Not Available")]
        NA = 0,
        [Description("<2 Hours")]
        UnderTwoHours = 1,
        [Description("2-10 Hours")]
        TwoToTenHours = 2,
        [Description("10-30 Hours")]
        TenToThirtyHours = 3,
        [Description("30-50 Hours")]
        ThirtyToFiftyHours = 4,
        [Description(">50 Hours")]
        OverFiftyHours = 5,
    }

#pragma warning disable 1591
    /// <summary>
    /// Map Wishlist status numbers to words.
    /// </summary>
    public enum WishlistStatus : long
    {
        None = -1,
        High = 0,
        Medium = 1,
        Low = 2,
        Blacklist = 3
    }

    /// <summary>
    /// Map Userlist status numbers to words.
    /// </summary>
    public enum UserlistStatus : long
    {
        None = -1,
        Unknown = 0,
        Playing = 1,
        Finished = 2,
        Stalled = 3,
        Dropped = 4
    }
#pragma warning restore 1591


}