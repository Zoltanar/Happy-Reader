using System;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core
{
    public class CoreSettings
    {
        /// <summary>
        /// Username of user.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// VNDB's UserID for user (found in the user's profile page).
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// Date of last time that dump files were downloaded.
        /// </summary>
        public DateTime DumpfileDate { get; set; }

        /// <summary>
        /// Date of last time that VNDB Stats were fetched.
        /// </summary>
        public DateTime StatsDate { get; set; }

        /// <summary>
        /// Date of last time that User's User-related titles (URT) were fetched.
        /// </summary>
        public DateTime URTDate { get; set; }

        /// <summary>
        /// Don't get titles released over a decade ago (does not apply to searched by name or favorite producer titles).
        /// </summary>
        public bool DecadeLimit { get; set; }

        /// <summary>
        /// Default constructor, sets all values to default.
        /// </summary>
        public CoreSettings()
        {
            Username = "guest";
            UserID = 0;
            DecadeLimit = true;
            DumpfileDate = DateTime.MinValue;
            StatsDate = DateTime.MinValue;
            URTDate = DateTime.MinValue;
        }

        public static CoreSettings Load() => LoadJson<CoreSettings>(CoreSettingsJson);

        public void Save() => this.SaveJson(CoreSettingsJson);
    }
}
