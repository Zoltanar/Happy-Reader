using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162


namespace Happy_Apps_Core
{
    public class VNDatabase
    {
        private readonly VNDatabaseContext _context = new VNDatabaseContext();

        /// <summary>
        /// Contains all VNs in database.
        /// </summary>
        public DbSet<ListedVN> VNList => _context.VisualNovels;

        public DbSet<UserVN> UserVNList => _context.UserVisualNovels;
        /// <summary>
        /// Contains all producers in local database
        /// </summary>
        public DbSet<ListedProducer> ProducerList => _context.Producers;

        /// <summary>
        /// Contains all characters in local database
        /// </summary>
        public DbSet<CharacterItem> CharacterList => _context.Characters;

        /// <summary>
        /// Contains all favorite producers for logged in user
        /// </summary>
        public ICollection<ListedProducer> FavoriteProducerList
        {
            get
            {

                var user = _context.Users.SingleOrDefault(x => x.Id == Settings.UserID);
                if (user == null)
                {
                    var nUser = new User { Id = Settings.UserID };
                    _context.Users.Add(nUser);
                    _context.SaveChanges();
                    user = _context.Users.Single(x => x.Id == Settings.UserID);
                }
                return user.FavoriteProducers;
            }
        }


        #region Initialization

        public VNDatabase()
        {
            LogToFile("VN Items= " + VNList.Count());
            LogToFile("Producers= " + ProducerList.Count());
            LogToFile("Characters= " + CharacterList.Count());
            LogToFile("UserVN Items= " + UserVNList.Count());
        }

        #endregion

        #region Set Methods

        public void AddNoteToVN(int vnid, string note, int userID)
        {
            var uvn = _context.UserVisualNovels.Single(x => x.UserId == userID && x.VNID == vnid);
            uvn.ULNote = note;
            SaveChanges();
        }

        public void AddRelationsToVN(int vnid, VNItem.RelationsItem[] relations)
        {
            var relationsString = relations.Any() ? ListToJsonArray(new List<object>(relations)) : "Empty";
            _context.VisualNovels.Single(x => x.VNID == vnid).Relations = relationsString;
            _context.SaveChanges();
        }

        public void AddScreensToVN(int vnid, VNItem.ScreenItem[] screens)
        {
            var screensString = screens.Any() ? ListToJsonArray(new List<object>(screens)) : "Empty";
            _context.VisualNovels.Single(x => x.VNID == vnid).Screens = screensString;
            _context.SaveChanges();
        }

        public void AddAnimeToVN(int vnid, VNItem.AnimeItem[] anime)
        {
            var animeString = anime.Any() ? ListToJsonArray(new List<object>(anime)) : "Empty";
            _context.VisualNovels.Single(x => x.VNID == vnid).Anime = animeString;
            _context.SaveChanges();
        }

        public void UpdateVNTagsStats(VNItem vnItem, bool saveChanges)
        {
            var tags = ListToJsonArray(new List<object>(vnItem.Tags));
            var vn = _context.VisualNovels.Single(x => x.VNID == vnItem.ID);
            vn.Tags = tags;
            vn.Popularity = vnItem.Popularity;
            vn.Rating = vnItem.Rating;
            vn.VoteCount = vnItem.VoteCount;
            if (saveChanges) _context.SaveChanges();
        }
        
        public void InsertFavoriteProducers(List<ListedProducer> addProducerList, int userid)
        {
            var user = _context.Users.Single(x => x.Id == userid);
            addProducerList.ForEach(user.FavoriteProducers.Add);
            _context.SaveChanges();
        }

        /// <summary>
        /// Insert or Replace Producer into producerlist, if adding for the first time, set date to null.
        /// </summary>
        /// <param name="producer">The producer to be inserted</param>
        /// <param name="setDateNull">Sets date to null rather than the default (which is CURRENT TIMESTAMP)</param>
        /// <param name="saveChanges">Commit changes to database if true, if false, then make sure to save yourself after</param>
        public void UpsertProducer(ProducerItem producer, bool setDateNull, bool saveChanges)
        {
            var dbProducer = _context.Producers.SingleOrDefault(x => x.ID == producer.ID);
            if (dbProducer == null)
            {
                dbProducer = new ListedProducer { ID = producer.ID };
                _context.Producers.Add(dbProducer);
            }
            dbProducer.Name = producer.Name;
            dbProducer.Language = producer.Language;
            dbProducer.UpdatedDt = null;
            if (!setDateNull) dbProducer.UpdatedDt = DateTime.UtcNow;
            if (saveChanges) SaveChanges();
        }

        /// <summary>
        /// Adds or Updates all titles in User-related title list.
        /// </summary>
        /// <param name="userid">ID of User</param>
        /// <param name="urtList">List of URT titles</param>
        public void UpdateURTTitles(int userid, IEnumerable<UrtListItem> urtList)
        {
            foreach (var item in urtList)
            {
                var uvn = _context.UserVisualNovels.SingleOrDefault(x => x.UserId == userid && x.VNID == item.ID);
                switch (item.Action)
                {
                    case Command.New:
                        uvn = new UserVN { VNID = item.ID };
                        _context.UserVisualNovels.Add(uvn);
                        goto case Command.Update;
                    case Command.Update:
                        Debug.Assert(uvn != null, nameof(uvn) + " != null");
                        uvn.ULStatus = item.ULStatus;
                        uvn.ULAdded = item.ULAdded;
                        uvn.ULNote = item.ULNote;
                        uvn.WLStatus = item.WLStatus;
                        uvn.WLAdded = item.WLAdded;
                        uvn.Vote = item.Vote;
                        uvn.VoteAdded = item.VoteAdded;
                        break;
                    case Command.Delete:
                        Debug.Assert(uvn != null, nameof(uvn) + " != null");
                        _context.UserVisualNovels.Remove(uvn);
                        break;
                }
                SaveChanges();
            }
        }

        public void UpsertSingleCharacter(CharacterItem character, bool saveChanges)
        {
            var dbCharacter = _context.Characters.SingleOrDefault(x => x.ID == character.ID);
            if (dbCharacter == null)
            {
                dbCharacter = new CharacterItem { ID = character.ID };
                _context.Characters.Add(dbCharacter);
            }
            dbCharacter.TraitsColumn = ListToJsonArray(new List<object>(character.Traits));
            dbCharacter.VNsColumn = ListToJsonArray(new List<object>(character.VNs));
            if (saveChanges) _context.SaveChanges();
        }

        public void UpsertSingleVN((VNItem item, ProducerItem producer, VNLanguages languages) data, bool setFullyUpdated, bool saveChanges)
        {
            var (item, producer, languages) = data;
            var vn = _context.VisualNovels.SingleOrDefault(x => x.VNID == item.ID);
            if (vn == null)
            {
                vn = new ListedVN() { VNID = item.ID };
                _context.VisualNovels.Add(vn);
            }
            vn.Title = item.Title;
            vn.KanjiTitle = item.Original;
            vn.ProducerID = producer?.ID;
            vn.RelDate = item.Released;
            vn.Tags = ListToJsonArray(new List<object>(item.Tags));
            vn.Description = item.Description;
            vn.ImageURL = item.Image;
            vn.ImageNSFW = item.Image_Nsfw;
            vn.LengthTime = (LengthFilter?)item.Length;
            vn.Popularity = item.Popularity;
            vn.Rating = item.Popularity;
            vn.VoteCount = item.VoteCount;
            vn.Aliases = item.Aliases;
            vn.Languages = languages?.ToString();
            if (setFullyUpdated) vn.DateFullyUpdated = DateTime.UtcNow;
            if (saveChanges) SaveChanges();
        }

        public void RemoveFavoriteProducer(int producerID, int userid)
        {
            _context.Users.Single(x => x.Id == userid).FavoriteProducers
                .Remove(_context.Producers.Single(x => x.ID == producerID));
            _context.SaveChanges();
        }

        public void RemoveVisualNovel(int vnid, bool saveChanges)
        {
            var vn = _context.VisualNovels.First(x => x.VNID == vnid);
            _context.VisualNovels.Remove(vn);
            if (saveChanges) _context.SaveChanges();
        }

        #endregion

        #region Other

        public static Func<ListedVN, bool> ListVNByNameOrAliasFunc(string searchString)
        {
            searchString = searchString.ToLowerInvariant();
            return vn =>
                vn.Title.ToLowerInvariant().Contains(searchString) ||
                (vn.KanjiTitle?.ToLowerInvariant().Contains(searchString) ?? false) ||
                (vn.Aliases?.ToLowerInvariant().Contains(searchString) ?? false);
        }

        /// <summary>
        ///     Type of VN status to be changed.
        /// </summary>
        public enum ChangeType
        {
            UL,
            WL,
            Vote
        }

        /// <summary>
        /// Object for updating user-related list.
        /// </summary>
        public class UrtListItem
        {
#pragma warning disable 1591
            public int ID { get; }
            public UserlistStatus? ULStatus { get; private set; }
            public int? ULAdded { get; private set; }
            public string ULNote { get; private set; }
            public WishlistStatus? WLStatus { get; private set; }
            public int? WLAdded { get; private set; }
            public int? Vote { get; private set; }
            public int? VoteAdded { get; private set; }
            public Command Action { get; private set; }
#pragma warning restore 1591

            /// <summary>
            /// Create URT item from previously fetched data. (For Method Group)
            /// </summary>
            public static UrtListItem FromVN(ListedVN vn)
            {
                return new UrtListItem(vn);
            }

            /// <summary>
            /// Create URT item from previously fetched data.
            /// </summary>
            public UrtListItem(ListedVN vn)
            {
                ID = vn.VNID;
                Action = Command.Delete;
            }

            /// <summary>
            /// Create new URT item from user list data.
            /// </summary>
            public UrtListItem(UserListItem item)
            {
                ID = item.VN;
                ULStatus = (UserlistStatus)item.Status;
                ULAdded = item.Added;
                ULNote = item.Notes;
                Action = Command.New;
            }

            /// <summary>
            /// Create new URT item from wish list data.
            /// </summary>
            public UrtListItem(WishListItem item)
            {
                ID = item.VN;
                WLStatus = (WishlistStatus)item.Priority;
                WLAdded = item.Added;
                Action = Command.New;
            }

            /// <summary>
            /// Create new URT item from vote list data.
            /// </summary>
            public UrtListItem(VoteListItem item)
            {
                ID = item.VN;
                Vote = item.Vote;
                VoteAdded = item.Added;
                Action = Command.New;
            }

            /// <summary>
            /// Update URT item with user list data.
            /// </summary>
            public void Update(UserListItem item)
            {
                ULStatus = (UserlistStatus)item.Status;
                ULAdded = item.Added;
                ULNote = item.Notes;
                Action = Command.Update;
            }


            /// <summary>
            /// Update URT item with wish list data.
            /// </summary>
            public void Update(WishListItem item)
            {
                WLStatus = (WishlistStatus)item.Priority;
                WLAdded = item.Added;
                Action = Command.Update;
            }

            /// <summary>
            /// Update URT item with vote list data.
            /// </summary>
            public void Update(VoteListItem item)
            {
                Vote = item.Vote;
                VoteAdded = item.Added;
                Action = Command.Update;
            }

            /// <summary>Returns a string that represents the current object.</summary>
            /// <returns>A string that represents the current object.</returns>
            /// <filterpriority>2</filterpriority>
            public override string ToString() => $"{Action} - {ID}";
        }

        /// <summary>
        ///     Command to change VN status.
        /// </summary>
        public enum Command
        {
            /// <summary>
            /// Add to URT list
            /// </summary>
            New,
            /// <summary>
            /// Update item in URT list
            /// </summary>
            Update,
            /// <summary>
            /// Delete item from URT list
            /// </summary>
            Delete
        }

        /// <summary>
        /// Convert list of objects to JSON array string.
        /// </summary>
        /// <param name="objects">List of objects</param>
        /// <returns>JSON array string</returns>
        private static string ListToJsonArray(ICollection<object> objects)
        {
            return JsonConvert.SerializeObject(objects);
        }

        #endregion

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }

}