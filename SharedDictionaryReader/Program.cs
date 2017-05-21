using Happy_Reader.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SharedDictionaryReader
{
    static class Program
    {

        static readonly HappyReaderDatabase Data = new HappyReaderDatabase();
        static void Main(string[] args)
        {
            var arg = args.FirstOrDefault();
            switch (arg)
            {
                case "gamedic":
                    ReadGamedic();
                    break;
                case "games":
                    ReadUserGames();
                    break;
                case "gameitems":
                    ReadGameItems();
                    break;
                case "gamefiles":
                    ReadGameFiles();
                    break;
                case "users":
                    ReadUsers();
                    break;
                default:
                    Console.WriteLine($"Argument {arg} not found.");
                    break;
            }
            Console.Write(@"Completed, press key to exit...");
            Console.ReadKey(true);
            Console.WriteLine();
        }

        // ReSharper disable PossibleNullReferenceException

        private static void ReadGameFiles()
        {
            var doc = new XmlDocument();
            doc.Load("gamefiles.xml");
            var termsNode = doc.SelectSingleNode("grimoire/games");
            var count = 0;
            var myGameFiles = new List<GameFile>();
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (XmlNode node in termsNode.ChildNodes)
            {
                var gameFile = new GameFile
                {
                    Id = Convert.ToInt64(node.Attributes["id"].InnerText),
                    MD5 = node.SelectSingleNode("md5")?.InnerText,
                    GameId = Convert.ToInt64(node.SelectSingleNode("itemId")?.InnerText)
                };
                myGameFiles.Add(gameFile);
                count++;
                Console.Write($"Processed {count}...");
                Console.CursorLeft = 0;
            }
            Console.WriteLine();
            Data.GameFiles.AddRange(myGameFiles);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Completed, found {myGameFiles.Count} games, saving to database...");
            Data.SaveChanges();
        }

        private static void ReadGameItems()
        {
            var doc = new XmlDocument();
            doc.Load("gameitems.xml");
            var termsNode = doc.SelectSingleNode("grimoire/items");
            //var fieldNames = typeof(XmlGameItem).GetProperties().Select(field => field.Name);
            var count = 0;
            var myGames = new List<Game>();
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (XmlNode node in termsNode.ChildNodes)
            {
                /*foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (!fieldNames.Contains(attribute.Name)) break;
                }
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (!fieldNames.Contains(child.Name)) break;
                }*/
                var game = new Game
                {
                    Id = Convert.ToInt64(node.Attributes["id"].InnerText),
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt32(node.SelectSingleNode("timestamp")?.InnerText)).UtcDateTime,
                    Title = node.SelectSingleNode("title").InnerText,
                    RomajiTitle = node.SelectSingleNode("romajiTitle")?.InnerText,
                    Brand = node.SelectSingleNode("brand")?.InnerText,
                    Series = node.SelectSingleNode("series")?.InnerText,
                    Image = node.SelectSingleNode("image")?.InnerText,
                    Banner = node.SelectSingleNode("banner")?.InnerText,
                    Wiki = node.SelectSingleNode("wiki")?.InnerText,
                    Tags = node.SelectSingleNode("tags")?.InnerText,
                    Date = node.SelectSingleNode("date")?.InnerText,
                    Artists = node.SelectSingleNode("artists")?.InnerText,
                    SDArtists = node.SelectSingleNode("sdArtists")?.InnerText,
                    Writers = node.SelectSingleNode("writers")?.InnerText,
                    Musicians = node.SelectSingleNode("musicians")?.InnerText,
                    Otome = Convert.ToBoolean(node.SelectSingleNode("otome")?.InnerText ?? bool.FalseString),
                    Ecchi = Convert.ToBoolean(node.SelectSingleNode("ecchi")?.InnerText ?? bool.FalseString),
                    Okazu = Convert.ToBoolean(node.SelectSingleNode("okazu")?.InnerText ?? bool.FalseString),
                };
                myGames.Add(game);
                count++;
                Console.Write($"Processed {count}...");
                Console.CursorLeft = 0;
            }
            Console.WriteLine();
            Data.Games.AddRange(myGames);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Completed, found {myGames.Count} games, saving to database...");
            Data.SaveChanges();
        }

        private static void ReadUsers()
        {
            var doc = new XmlDocument();
            doc.Load("users.xml");
            var termsNode = doc.SelectSingleNode("grimoire/users");
            //var fieldNames = typeof(XmlUser).GetProperties().Select(field => field.Name);
            var count = 0;
            var myUsers = new List<User>();
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (XmlNode node in termsNode.ChildNodes)
            {
                /*foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (!fieldNames.Contains(attribute.Name)) break;
                }
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (!fieldNames.Contains(child.Name)) break;
                }*/
                var user = new User
                {
                    Id = Convert.ToInt64(node.Attributes["id"].InnerText),
                    Username = node.SelectSingleNode("name").InnerText,
                    Language = node.SelectSingleNode("language").InnerText,
                    Gender = node.SelectSingleNode("gender")?.InnerText,
                    Homepage = node.SelectSingleNode("homepage")?.InnerText,
                    Avatar = node.SelectSingleNode("avatar")?.InnerText,
                    Color = node.SelectSingleNode("color")?.InnerText,
                    TermLevel = int.TryParse(node.SelectSingleNode("termLevel")?.InnerText, out int tl) ? (int?)tl : null,//Convert.ToInt32(node.SelectSingleNode("termLevel").InnerText),
                    CommentLevel = int.TryParse(node.SelectSingleNode("commentLevel")?.InnerText, out int cl) ? (int?)cl : null
                };
                myUsers.Add(user);
                count++;
                Console.Write($"Processed {count}...");
                Console.CursorLeft = 0;
            }
            Console.WriteLine();
            Data.Users.AddRange(myUsers);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Completed, found {myUsers.Count} users, saving to database...");
            Data.SaveChanges();
        }

        private static void ReadUserGames()
        {
            var doc = new XmlDocument();
            doc.Load("games.xml");
            var termsNode = doc.SelectSingleNode("grimoire/games");
            //var fieldNames = typeof(XmlUserGame).GetProperties().Select(field => field.Name);
            var count = 0;
            var myGames = new List<UserGame>();
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (XmlNode node in termsNode.ChildNodes)
            {
                /*foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (!fieldNames.Contains(attribute.Name)) break;
                }
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (!fieldNames.Contains(child.Name)) break;
                }*/
                var udn = node.SelectSingleNode("userDefinedName")?.InnerText;
                var lang = node.SelectSingleNode("language").InnerText;
                var wndn = node.SelectSingleNode("names/name[@type='window']").InnerText;
                var fldn = node.SelectSingleNode("names/name[@type='folder']").InnerText;
                var fln = node.SelectSingleNode("names/name[@type='file']").InnerText;
                var game = new UserGame
                {
                    Id = Convert.ToInt64(node.Attributes["id"].InnerText),
                    UserDefinedName = udn,
                    Language = lang,
                    WindowName = wndn,
                    FolderName = fldn,
                    FileName = fln,
                    IgnoresRepeat = Convert.ToBoolean(node.SelectSingleNode("ignoresRepeat")?.InnerText ?? bool.FalseString)
                };
                if (game.Id == 0)
                {
                    continue;
                }
                myGames.Add(game);
                count++;
                Console.Write($"Processed {count}...");
                Console.CursorLeft = 0;
            }
            Console.WriteLine();
            Data.UserGames.AddRange(myGames);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Completed, found {myGames.Count} user games, saving to database...");
            Data.SaveChanges();
        }

        private static void ReadGamedic()
        {
            var doc = new XmlDocument();
            doc.Load("gamedic.xml");
            var termsNode = doc.SelectSingleNode("grimoire/terms");
            //var fieldNames = typeof(XmlEntry).GetProperties().Select(field => field.Name);
            var count = 0;
            var myEntries = new List<Entry>();
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (XmlNode node in termsNode.ChildNodes)
            {
                /*foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (!fieldNames.Contains(attribute.Name)) break;
                }
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (!fieldNames.Contains(child.Name)) break;
                }*/
                var entry = new Entry
                {
                    Id = Convert.ToInt64(node.Attributes["id"].InnerText),
                    Type = (EntryType)Enum.Parse(typeof(EntryType), node.Attributes["type"].InnerText, true),
                    Disabled = Convert.ToBoolean(node.Attributes["disabled"]?.InnerText ?? bool.FalseString),
                    UserId = Convert.ToInt64(node.SelectSingleNode("userId").InnerText),
                    UserHash = Convert.ToInt32(node.SelectSingleNode("userHash")?.InnerText),
                    FileId = Convert.ToInt64(node.SelectSingleNode("gameId")?.InnerText),
                    Host = node.SelectSingleNode("host")?.InnerText,
                    FromLanguage = node.SelectSingleNode("sourceLanguage").InnerText,
                    ToLanguage = node.SelectSingleNode("language").InnerText,
                    Time = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt32(node.SelectSingleNode("timestamp").InnerText)).UtcDateTime,
                    UpdateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt32(node.SelectSingleNode("updateTimestamp")?.InnerText)).UtcDateTime,
                    UpdateUserId = Convert.ToInt64(node.SelectSingleNode("updateUserId")?.InnerText),
                    Priority = Convert.ToDouble(node.SelectSingleNode("priority")?.InnerText),
                    CaseInsensitive = Convert.ToBoolean(node.SelectSingleNode("icase")?.InnerText ?? bool.FalseString),
                    SeriesSpecific = Convert.ToBoolean(node.SelectSingleNode("special")?.InnerText ?? bool.FalseString),
                    PhraseBoundary = Convert.ToBoolean(node.SelectSingleNode("phrase")?.InnerText ?? bool.FalseString),
                    Regex = Convert.ToBoolean(node.SelectSingleNode("regex")?.InnerText ?? bool.FalseString),
                    Hentai = Convert.ToBoolean(node.SelectSingleNode("hentai")?.InnerText ?? bool.FalseString),
                    Private = Convert.ToBoolean(node.SelectSingleNode("private")?.InnerText ?? bool.FalseString),
                    Context = node.SelectSingleNode("context")?.InnerText,
                    RoleString = node.SelectSingleNode("role")?.InnerText,
                    Input = node.SelectSingleNode("pattern").InnerText,
                    Output = node.SelectSingleNode("text")?.InnerText ?? "",
                    Ruby = node.SelectSingleNode("ruby")?.InnerText,
                    Comment = node.SelectSingleNode("comment")?.InnerText,
                    UpdateComment = node.SelectSingleNode("updateComment")?.InnerText
                };
                entry.GameId = Data.GameFiles.SingleOrDefault(i => i.Id == entry.FileId)?.GameId;
                myEntries.Add(entry);
                count++;
                Console.Write($"Processed {count}...");
                Console.CursorLeft = 0;
            }
            Data.Entries.AddRange(myEntries);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Completed, found {myEntries.Count} terms, saving to database...");
            Data.SaveChanges();
        }

        // ReSharper restore PossibleNullReferenceException
    }

    //these classes were used to ensure all attributes/nodes in xml were known

    #region XmlClasses
    // ReSharper disable All
#pragma warning disable IDE1006 // Naming Styles
    internal class XmlGameItem
    {
        public int id { get; set; }

        public int timestamp { get; set; }
        public string title { get; set; }
        public string romajiTitle { get; set; }
        public string brand { get; set; }
        public string series { get; set; }
        public string image { get; set; }
        public string banner { get; set; }
        public string wiki { get; set; }
        public string tags { get; set; }
        public string fileSize { get; set; }
        public string date { get; set; }
        public string artists { get; set; }
        public string sdartists { get; set; }
        public string musicians { get; set; }
        public string writers { get; set; }
        public bool otome { get; set; }
        public bool ecchi { get; set; }
        public bool okazu { get; set; }
        public int topicCount { get; set; }
        public int annotCount { get; set; }
        public int playUserCount { get; set; }
        public int scapeMedian { get; set; }
        public int scapeCount { get; set; }
        public int ecchiScoreCount { get; set; }
        public int ecchiScoreSum { get; set; }
        public int overallScoreCount { get; set; }
        public int overallScoreSum { get; set; }
        public int subtitleCount { get; set; }
    }

    internal class XmlUser
    {
        public int id { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
        public string language { get; set; }
        public string homepage { get; set; }
        public string avatar { get; set; }
        public string color { get; set; }
        public int termLevel { get; set; }
        public int commentLevel { get; set; }
    }

    internal class XmlUserGame
    {
        public int id { get; set; }
        public string md5 { get; set; }
        public string path { get; set; }
        public int itemId { get; set; }
        public string userDefinedName { get; set; }
        public string language { get; set; }
        public string encoding { get; set; }
        public string deletedHook { get; set; }
        public string threadKept { get; set; }
        public int threads { get; set; }
        public int names { get; set; }
        public string window { get; set; }
        public string folder { get; set; }
        public string file { get; set; }
        public int visitTime { get; set; }
        public int visitCount { get; set; }
        public int commentCount { get; set; }
        public int commentsUpdateTime { get; set; }
        public int refsUpdateTime { get; set; }
        public bool gameAgentDisabled { get; set; }
        public string hook { get; set; }
        public bool ignoresRepeat { get; set; }
        public string launchPath { get; set; }
    }

    internal class XmlEntry
    {
        public int id { get; set; }
        public string type { get; set; }
        public bool disabled { get; set; }
        public int userId { get; set; }
        public int? userHash { get; set; }
        public int? gameId { get; set; }
        public string host { get; set; }
        public string sourceLanguage { get; set; }
        public string language { get; set; }
        public int timestamp { get; set; }
        public int? updateTimestamp { get; set; }
        public string updateComment { get; set; }
        public int? updateUserId { get; set; }
        public double? priority { get; set; }
        public bool icase { get; set; }
        public bool special { get; set; }
        public bool phrase { get; set; }
        public bool regex { get; set; }
        public bool hentai { get; set; }
        public bool @private { get; set; }
        public string context { get; set; }
        public string role { get; set; }
        public string pattern { get; set; }
        public string text { get; set; }
        public string ruby { get; set; }
        public string comment { get; set; }
    }
    // ReSharper restore All
#pragma warning restore IDE1006 // Naming Styles
    #endregion

}
