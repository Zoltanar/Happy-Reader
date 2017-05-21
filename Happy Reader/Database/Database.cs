using System.Linq;

namespace Happy_Reader.Database
{
    public partial class HappyReaderDatabase
    {
        /// <summary>
        /// Tries to get user by username, returns null if not found.
        /// </summary>
        public User GetUser(string userName) => Users.FirstOrDefault(i => i.Username == userName);

        /// <summary>
        /// Tries to get game by title first, if not found then by romajiTitle, returns null if not found.
        /// </summary>
        public Game GetGameByName(string name) => Games.FirstOrDefault(i => i.Title == name) ?? Games.FirstOrDefault(i => i.RomajiTitle == name);
        
    }

    public enum EntryType
    {
        // ReSharper disable All
        Proxy = -40,
        Macro = -30,
        OCR = -20,
        TTS = -10,
        //stage zero
        Game = 0,
        //stage 1
        Input = 10,
        Yomi = 20,
        //stage 2
        Translation = 30,
        Trans = 30,
        Name = 40,
        ProxyMod = 41,
        Prefix = 45,
        Suffix = 46,
        //stage 3
        Output = 50,
        // ReSharper restore All
    }
}
