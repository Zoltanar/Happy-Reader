using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162


namespace Happy_Apps_Core
{
    public partial class VNDatabase
    {
        private readonly VNDatabaseContext _context = new VNDatabaseContext();

        /// <summary>
        /// Contains all VNs in database.
        /// </summary>
        public DbSet<ListedVN> VNList => _context.VisualNovels;

        /// <summary>
        /// Contains all producers in local database
        /// </summary>
        public DbSet<ListedProducer> ProducerList => _context.Producers;

        /// <summary>
        /// Contains all characters in local database
        /// </summary>
        public DbSet<CharacterItem> CharacterList => _context.Characters;
        
        /// <summary>
        /// Contains all characters in local database
        /// </summary>
        public IQueryable<ListedVN> URTList
        {
            get
            {
                return _context.UserVisualNovels.Where(x => x.UserId == Settings.UserID).Select(x=> x.ListedVN);
            }
        }

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

        private readonly string _dbFile;
        protected readonly SQLiteConnection Conn;
        private static bool _dbLog;
        
        public VNDatabase(string dbFile = null, bool dbLog = false)
        {
            _dbFile = dbFile;
            if (string.IsNullOrWhiteSpace(_dbFile))
            {
#if DEBUG
        _dbFile = "..\\Release\\Stored Data\\Happy-Search-Local-DB.sqlite";
#else
        _dbFile = "Stored Data\\Happy-Search-Local-DB.sqlite";
#endif
            }
            var dbConnectionString = "Data Source=" + _dbFile + ";Version=3;";
            _dbLog = dbLog;
            _printSetMethods = dbLog;
            PrintGetMethods = dbLog;

            Conn = new SQLiteConnection(dbConnectionString);
            InitDatabase();

            Open();
            Close();
            LogToFile("VN Items= " + VNList.Count());
            LogToFile("Producers= " + ProducerList.Count());
            LogToFile("Characters= " + CharacterList.Count());
            LogToFile("UserRelated Items= " + URTList.Count());
        }
        
        private void InitDatabase()
        {
            if (File.Exists(_dbFile))
            {
                //check database version and update if necessary.
                Open();
                var version = GetCurrentVersion();
                if (version < DatabaseVersion.Latest)
                {
                    if (_dbLog) LogToFile("Updating Database");
                    UpgradeDatabase(version);
                    if (_dbLog) LogToFile("Finished Updating Database");
                }
                Close();
                return;
            }
            SQLiteConnection.CreateFile(_dbFile);
            if (_dbLog) LogToFile("Creating Database");
            BeginTransaction();
            //must be in this order
            CreateProducerListTable();
            CreateVNListTable();
            CreateCharacterListTable();
            CreateUserlistTable();
            CreateUserProdListTable();
            CreateTableDetails();
            CreateTriggers();
            EndTransaction();
            if (_dbLog) LogToFile("Finished Creating Database");
        }

        private DatabaseVersion GetCurrentVersion()
        {
            //check if table exists
            var selectString = "SELECT name FROM sqlite_master WHERE type='table' AND name='tabledetails';";
            if (PrintGetMethods) LogToFile(selectString);
            var command = new SQLiteCommand(selectString, Conn);
            var returned = command.ExecuteScalar();
            if (returned == null) return DatabaseVersion.Pre;
            //check table version
            selectString = "SELECT Value FROM tabledetails WHERE Key='databaseversion';";
            if (PrintGetMethods) LogToFile(selectString);
            command = new SQLiteCommand(selectString, Conn);
            var version = command.ExecuteScalar().ToString();
            switch (version)
            {
                case "V1_4_0":
                    return DatabaseVersion.V1_4_0;
                case "V1_4_7":
                    return DatabaseVersion.V1_4_7;
                default:
                    return DatabaseVersion.Pre;
            }
        }

        private void UpgradeDatabase(DatabaseVersion current)
        {
            try
            {
                BackupPreviousDatabase(current);
            }
            catch (Exception ex) when (ex is PathTooLongException || ex is UnauthorizedAccessException)
            {
                LogToFile("Couldn't backup previous version.",ex);
            }
            if (current < DatabaseVersion.V1_4_0)
            {
                //remove update producerlist date trigger
                var commandString = "DROP TRIGGER UpdateTimestampProducerList;";
                if (PrintGetMethods) LogToFile(commandString);
                var command = new SQLiteCommand(commandString, Conn);
                command.ExecuteNonQuery();
                //set Updated values to null
                commandString = "UPDATE producerlist SET Updated = NULL;";
                if (PrintGetMethods) LogToFile(commandString);
                command = new SQLiteCommand(commandString, Conn);
                command.ExecuteNonQuery();
                //create tabledetails table
                CreateTableDetails();
            }
            if (current < DatabaseVersion.V1_4_7)
            {
                //Add columns to vnlist table (aliases, languages)
                var commandString = @"ALTER TABLE vnlist
  ADD Aliases TEXT;
ALTER TABLE vnlist
 ADD Languages TEXT;
ALTER TABLE vnlist
 ADD DateFullyUpdated DATE;";
                if (PrintGetMethods) LogToFile(commandString);
                var command = new SQLiteCommand(commandString, Conn);
                command.ExecuteNonQuery();
                commandString = @"ALTER TABLE producerlist ADD Language TEXT;";
                if (PrintGetMethods) LogToFile(commandString);
                command = new SQLiteCommand(commandString, Conn);
                command.ExecuteNonQuery();
            }
            SetDbVersion(DatabaseVersion.Latest);
        }

        private void BackupPreviousDatabase(DatabaseVersion previous)
        {
            if (!File.Exists(_dbFile)) return;
            Debug.Assert(_dbFile != null, nameof(_dbFile) + " != null");
            var extLength = Path.GetExtension(_dbFile).Length;
            var backupFile = $"{_dbFile.Substring(0, _dbFile.Length - extLength)} - {previous} Backup.sqlite";
            if (File.Exists(backupFile)) File.Delete(backupFile);
            File.Copy(_dbFile, backupFile);
        }

        private void CreateTableDetails()
        {
            const string createCommand =
                @"CREATE TABLE `tabledetails` (
	`Key`	TEXT NOT NULL,
	`Value`	TEXT,
	PRIMARY KEY(`Key`)
);";
            var command = new SQLiteCommand(createCommand, Conn);
            command.ExecuteNonQuery();
            string insertCommand = @"INSERT INTO tabledetails  (Key,Value) VALUES ('databaseversion','V1_4_7');";
            command = new SQLiteCommand(insertCommand, Conn);
            command.ExecuteNonQuery();
            insertCommand = @"INSERT INTO tabledetails  (Key,Value) VALUES ('programname','Happy Search');";
            command = new SQLiteCommand(insertCommand, Conn);
            command.ExecuteNonQuery();
            insertCommand = @"INSERT INTO tabledetails  (Key,Value) VALUES ('author','zoltanar');";
            command = new SQLiteCommand(insertCommand, Conn);
            command.ExecuteNonQuery();
            insertCommand = $@"INSERT INTO tabledetails  (Key,Value) VALUES ('projecturl','{ProjectURL}');";
            command = new SQLiteCommand(insertCommand, Conn);
            command.ExecuteNonQuery();
        }

        private void SetDbVersion(DatabaseVersion version)
        {
            string versionString;
            switch (version)
            {
                case DatabaseVersion.V1_4_0:
                    versionString = "V1_4_0";
                    break;
                case DatabaseVersion.V1_4_7:
                    versionString = "V1_4_7";
                    break;
                default:
                    return;
            }
            string insertCommand = $@"INSERT OR REPLACE INTO tabledetails  (Key,Value) VALUES ('databaseversion','{versionString}');";
            var command = new SQLiteCommand(insertCommand, Conn);
            command.ExecuteNonQuery();
        }

        private void CreateVNListTable()
        {
            const string createCommand = @"CREATE TABLE ""vnlist"" ( 
`VNID` INTEGER NOT NULL UNIQUE, 
`Title` TEXT, 
`KanjiTitle` TEXT, 
`RelDate` TEXT, 
`ProducerID` INTEGER, 
`Tags` TEXT, 
`DateUpdated` DATE DEFAULT CURRENT_TIMESTAMP, 
`ImageURL` TEXT, 
`ImageNSFW` INTEGER, 
`Description` TEXT, 
`LengthTime` INTEGER, 
`Popularity` NUMERIC, 
`Rating` NUMERIC, 
`VoteCount` INTEGER, 
`Relations` TEXT, 
`Screens` TEXT, 
`Anime` TEXT, 
`Aliases` TEXT, 
`Languages` TEXT, 
`DateFullyUpdated` DATE, 
PRIMARY KEY(`VNID`), 
FOREIGN KEY(`ProducerID`) REFERENCES `ProducerID` )";
            var command = new SQLiteCommand(createCommand, Conn);
            command.ExecuteNonQuery();
        }

        private void CreateProducerListTable()
        {
            const string createCommand = @"CREATE TABLE `producerlist` (
	`ProducerID`	INTEGER NOT NULL,
	`Name`	TEXT,
	`Titles`	INTEGER,
	`Loaded`	TEXT,
	`Updated`	DATE DEFAULT CURRENT_TIMESTAMP,
	`Language`	TEXT,
	PRIMARY KEY(`ProducerID`)
);";
            var command = new SQLiteCommand(createCommand, Conn);
            command.ExecuteNonQuery();
        }

        private void CreateCharacterListTable()
        {
            var createCommand = @"CREATE TABLE ""charlist"" ( 
`CharacterID` INTEGER NOT NULL UNIQUE, 
`Name` TEXT, 
`Image` TEXT, 
`Traits` TEXT, 
`VNs` TEXT, 
`DateUpdated` INTEGER, 
PRIMARY KEY(`CharacterID`) )";
            var command = new SQLiteCommand(createCommand, Conn);
            command.ExecuteNonQuery();
        }

        private void CreateUserlistTable()
        {
            const string createCommand = @"CREATE TABLE `userlist` (
 `VNID`	INTEGER,
 `UserID`	INTEGER,
 `ULStatus`	INTEGER,
 `ULAdded`	INTEGER,
 `ULNote`	TEXT,
 `WLStatus`	INTEGER,
 `WLAdded`	INTEGER,
 `Vote`	INTEGER,
 `VoteAdded`	INTEGER,
 PRIMARY KEY(VNID,UserID)
)";
            var command = new SQLiteCommand(createCommand, Conn);
            command.ExecuteNonQuery();
        }

        private void CreateUserProdListTable()
        {
            const string createCommand = @"CREATE TABLE `userprodlist`(
	`ProducerID`	INTEGER NOT NULL,
	`UserID`	INTEGER NOT NULL,
	`UserAverageVote`	NUMERIC,
	`UserDropRate`	INTEGER,
	PRIMARY KEY(ProducerID, UserID)
)";
            var command = new SQLiteCommand(createCommand, Conn);
            command.ExecuteNonQuery();
        }

        private void CreateTriggers()
        {
            const string createCommand = @"CREATE TRIGGER [UpdateTimestamp]
    AFTER UPDATE    ON vnlist    FOR EACH ROW
BEGIN
    UPDATE vnlist 
	SET DateUpdated=CURRENT_TIMESTAMP
	WHERE VNID=OLD.VNID;
END";
            const string createCommand3 = @"CREATE TRIGGER [UpdateTimestampCharacterList] 
AFTER UPDATE ON charlist FOR EACH ROW 
BEGIN 
UPDATE charlist SET DateUpdated=CURRENT_TIMESTAMP 
WHERE CharacterID=OLD.CharacterID; 
END";
            var command = new SQLiteCommand(createCommand, Conn);
            command.ExecuteNonQuery();
            var command3 = new SQLiteCommand(createCommand3, Conn);
            command3.ExecuteNonQuery();
        }

        #endregion

        #region Set Methods

        private static bool _printSetMethods;

        public void AddNoteToVN(int vnid, string note, int userID)
        {
            var noteString = Regex.Replace(note, "'", "''");
            var insertString =
                $"UPDATE userlist SET ULNote = '{noteString}' WHERE VNID = {vnid} AND UserID = {userID};";
            if (_printSetMethods) LogToFile(insertString);
            var command = new SQLiteCommand(insertString, Conn);
            command.ExecuteNonQuery();
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

        public void UpdateVNTagsStats(VNItem vnItem)
        {
            var tags = ListToJsonArray(new List<object>(vnItem.Tags));
            var vn = _context.VisualNovels.Single(x => x.VNID == vnItem.ID);
            vn.Tags = tags;
            vn.Popularity = vnItem.Popularity;
            vn.Rating = vnItem.Rating;
            vn.VoteCount = vnItem.VoteCount;
            _context.SaveChanges();
        }

        public void UpdateVNStatus(int userID, int vnid, ChangeType type, int statusInt, Command command, double newVoteValue = -1)
        {
            if (command == Command.Delete)
            {
                string deleteString = $"DELETE FROM userlist WHERE VNID = {vnid} AND UserID = {userID};";
                if (_printSetMethods) LogToFile(deleteString);
                var cmd = new SQLiteCommand(deleteString, Conn);
                cmd.ExecuteNonQuery();
                return;
            }
            var commandString = "";
            var statusDate = statusInt == -1
                ? "NULL"
                : DateTimeToUnixTimestamp(DateTime.UtcNow).ToString(CultureInfo.InvariantCulture);
            switch (type)
            {
                case ChangeType.UL:
                    switch (command)
                    {
                        case Command.New:
                            commandString =
                                $"INSERT OR REPLACE INTO userlist (VNID, UserID, ULStatus, ULAdded) Values ({vnid}, {userID}, {statusInt}, {statusDate});";
                            break;
                        case Command.Update:
                            commandString =
                                $"UPDATE userlist SET ULStatus = {statusInt}, ULAdded = {statusDate} WHERE VNID = {vnid} AND UserID = {userID};";
                            break;
                    }
                    break;
                case ChangeType.WL:
                    switch (command)
                    {
                        case Command.New:
                            commandString =
                                $"INSERT OR REPLACE  INTO userlist (VNID, UserID, WLStatus, WLAdded) Values ({vnid}, {userID}, {statusInt}, {statusDate});";
                            break;
                        case Command.Update:
                            commandString =
                                $"UPDATE userlist SET WLStatus = {statusInt}, WLAdded = {statusDate} WHERE VNID = {vnid} AND UserID = {userID};";
                            break;
                    }
                    break;
                case ChangeType.Vote:
                    int vote = (int)Math.Abs(newVoteValue * 10);
                    switch (command)
                    {
                        case Command.New:
                            commandString =
                                "INSERT OR REPLACE  INTO userlist (VNID, UserID, Vote, VoteAdded) " +
                                $"Values ({vnid}, {userID}, {vote}, {statusDate});";
                            break;
                        case Command.Update:
                            commandString =
                                $"UPDATE userlist SET Vote = {(statusInt != -1 ? vote.ToString() : "NULL")}, VoteAdded = {statusDate} WHERE VNID = {vnid} AND UserID = {userID};";
                            break;
                    }
                    break;
            }
            if (commandString.Equals("")) return;
            if (_printSetMethods) LogToFile(commandString);
            var cmd2 = new SQLiteCommand(commandString, Conn);
            cmd2.ExecuteNonQuery();
        }

        public void InsertFavoriteProducers(List<ListedProducer> addProducerList, int userid)
        {
            var user = _context.Users.Single(x => x.Id == userid);
            addProducerList.ForEach(user.FavoriteProducers.Add);
            _context.SaveChanges();
        }
        
        public bool IsBusy()
        {
            return Conn.State != ConnectionState.Closed;
        }

        /// <summary>
        /// Insert or Replace Producer into producerlist, if adding for the first time, set date to null.
        /// </summary>
        /// <param name="producer">The producer to be inserted</param>
        /// <param name="setDateNull">Sets date to null rather than the default (which is CURRENT TIMESTAMP)</param>
        public void InsertProducer(ListedProducer producer, bool setDateNull = false)
        {
            var name = Regex.Replace(producer.Name, "'", "''");
            var commandString = setDateNull ?
                $"INSERT OR REPLACE INTO producerlist (ProducerID, Name, Language, Titles, Updated) VALUES ({producer.ID}, '{name}', '{producer.Language}', {producer.NumberOfTitles}, NULL);" :
                $"INSERT OR REPLACE INTO producerlist (ProducerID, Name, Language, Titles) VALUES ({producer.ID}, '{name}', '{producer.Language}', {producer.NumberOfTitles});";
            if (_printSetMethods) LogToFile(commandString);
            var cmd = new SQLiteCommand(commandString, Conn);
            cmd.ExecuteNonQuery();
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
                var comm = new SQLiteCommand(Conn);
                switch (item.Action)
                {
                    case Command.New:
                    case Command.Update:
                        comm.CommandText =
                            "INSERT OR REPLACE INTO userlist (VNID, UserID, ULStatus, ULAdded, ULNote, WLStatus, WLAdded, Vote, VoteAdded)" +
                            "VALUES (@id, @uid, @uls, @ula, @uln, @wls, @wla, @vo, @va);";
                        comm.Parameters.Add(new SQLiteParameter("@uls", item.ULStatus));
                        comm.Parameters.Add(new SQLiteParameter("@ula", item.ULAdded));
                        comm.Parameters.Add(new SQLiteParameter("@uln", item.ULNote));
                        comm.Parameters.Add(new SQLiteParameter("@wls", item.WLStatus));
                        comm.Parameters.Add(new SQLiteParameter("@wla", item.WLAdded));
                        comm.Parameters.Add(new SQLiteParameter("@vo", item.Vote));
                        comm.Parameters.Add(new SQLiteParameter("@va", item.VoteAdded));
                        break;
                    case Command.Delete:
                        comm.CommandText = "DELETE FROM userlist WHERE VNID = @id AND UserID = @uid";
                        break;
                }
                comm.Parameters.Add(new SQLiteParameter("@id", item.ID));
                comm.Parameters.Add(new SQLiteParameter("@uid", userid));
                comm.ExecuteNonQuery();
            }
        }

        public void UpsertSingleCharacter(CharacterItem character)
        {
            var insertString =
                $"INSERT OR REPLACE INTO charlist (CharacterID, Traits, VNs) VALUES ('{character.ID}', '{ListToJsonArray(new List<object>(character.Traits))}','{ListToJsonArray(new List<object>(character.VNs))}');";
            if (_printSetMethods) LogToFile(insertString);
            var command = new SQLiteCommand(insertString, Conn);
            command.ExecuteNonQuery();
        }

        public void UpsertSingleVN((VNItem item, ProducerItem producer, VNLanguages languages) data, bool setFullyUpdated)
        {
            var (item, producer, languages) = data;
            var tags = ListToJsonArray(new List<object>(item.Tags));
            var command = new SQLiteCommand(Conn);
            if (setFullyUpdated)
            {
                command.CommandText = "INSERT OR REPLACE INTO vnlist (" +
                   "Title, KanjiTitle, ProducerID, RelDate, Tags, Description, " +
                   "ImageURL, ImageNSFW, LengthTime, " +
                   "Popularity, Rating, " +
                   "VoteCount, VNID, Aliases, Languages, DateFullyUpdated)" +
                   "VALUES(" +
                   "@title, @kanjititle, @producerid, @reldate, @tags, @description, " +
                   "@imageurl, @imagensfw, @lengthtime, " +
                   "@popularity,@rating, " +
                   "@votecount, @vnid, @aliases, @languages, @datefullyupdated);";
                command.Parameters.Add(new SQLiteParameter("@datefullyupdated", DateTime.UtcNow));
            }
            else
            {
                command.CommandText = "INSERT OR REPLACE INTO vnlist (" +
                   "Title, KanjiTitle, ProducerID, RelDate, Tags, Description, " +
                   "ImageURL, ImageNSFW, LengthTime, " +
                   "Popularity, Rating, " +
                   "VoteCount, VNID, Aliases, Languages)" +
                   "VALUES(" +
                   "@title, @kanjititle, @producerid, @reldate, @tags, @description, " +
                   "@imageurl, @imagensfw, @lengthtime, " +
                   "@popularity,@rating, " +
                   "@votecount, @vnid, @aliases, @languages);";
            }
            command.Parameters.Add(new SQLiteParameter("@title", item.Title));
            command.Parameters.Add(new SQLiteParameter("@kanjititle", item.Original));
            command.Parameters.Add(new SQLiteParameter("@producerid", producer?.ID ?? -1));
            command.Parameters.Add(new SQLiteParameter("@reldate", item.Released));
            command.Parameters.Add(new SQLiteParameter("@tags", tags));
            command.Parameters.Add(new SQLiteParameter("@description", item.Description));
            command.Parameters.Add(new SQLiteParameter("@imageurl", item.Image));
            command.Parameters.Add(new SQLiteParameter("@imagensfw", item.Image_Nsfw));
            command.Parameters.Add(new SQLiteParameter("@lengthtime", item.Length));
            command.Parameters.Add(new SQLiteParameter("@popularity", item.Popularity));
            command.Parameters.Add(new SQLiteParameter("@rating", item.Rating));
            command.Parameters.Add(new SQLiteParameter("@votecount", item.VoteCount));
            command.Parameters.Add(new SQLiteParameter("@vnid", item.ID));
            command.Parameters.Add(new SQLiteParameter("@aliases", item.Aliases));
            command.Parameters.Add(new SQLiteParameter("@languages", languages?.ToString()));
            if (_printSetMethods) LogToFile(command.CommandText);
            command.ExecuteNonQuery();
        }

        public void SetProducerLanguage(ListedProducer producer)
        {
            var insertString =
                $"UPDATE producerlist SET Language = '{producer.Language}' WHERE ProducerID = {producer.ID};";
            if (_printSetMethods) LogToFile(insertString);
            var command = new SQLiteCommand(insertString, Conn);
            command.ExecuteNonQuery();
        }

        public void RemoveFavoriteProducer(int producerID, int userid)
        {
            var commandString = $"DELETE FROM userprodlist WHERE ProducerID={producerID} AND UserID={userid};";
            if (_printSetMethods) LogToFile(commandString);
            var command = new SQLiteCommand(commandString, Conn);
            command.ExecuteNonQuery();
        }

        public void RemoveVisualNovel(int vnid)
        {
            var vn = _context.VisualNovels.First(x => x.VNID == vnid);
            _context.VisualNovels.Remove(vn);
            _context.SaveChanges();
        }

        #endregion

        #region Get Methods

        protected static bool PrintGetMethods;
        
        #endregion

        #region Other


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
        
        public void Open()
        {
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
                if (_dbLog) LogToFile("Opened Database");
            }
            else
            {
                LogToFile("Tried to open database, it was already opened.");
            }
        }

        public void Close()
        {
            Conn.Close();
            if (_dbLog) LogToFile("Closed Database");
        }

        private SQLiteTransaction _transaction;
        public void BeginTransaction()
        {
            Open();
            _transaction = Conn.BeginTransaction();
            if (_dbLog) LogToFile("Started Transaction");
        }

        public void EndTransaction()
        {
            _transaction.Commit();
            if (_dbLog) LogToFile("Commited Transaction");
            _transaction = null;
            Close();
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

        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Contains versions of the DB table.
        /// </summary>
        private enum DatabaseVersion
        {
            Pre = 0,
            V1_4_0 = 1,
            V1_4_7 = 2,
            Latest = 2
        }
        // ReSharper restore InconsistentNaming
        #endregion
    }

}