using System;

namespace Happy_Apps_Core
{
    public class CoreSettings : SettingsJsonFile
    {
        private string _username;
        private int _userID;
        private DateTime _dumpfileDate;
        private DateTime _statsDate;
        private DateTime _urtDate;
        private bool _decadeLimit;

        /// <summary>
        /// Username of user.
        /// </summary>
        public string Username
        {
            get => _username;
            set
            {
                if (_username == value) return;
                _username = value;
                if (Loaded) Save();
            }
        }

        /// <summary>
        /// VNDB's UserID for user (found in the user's profile page).
        /// </summary>
        public int UserID
        {
            get => _userID;
            set
            {
                if (_userID == value) return;
                _userID = value;
                if (Loaded) Save();
            }
        }

        /// <summary>
        /// Date of last time that dump files were downloaded.
        /// </summary>
        public DateTime DumpfileDate
        {
            get => _dumpfileDate;
            set
            {
                if (_dumpfileDate == value) return;
                _dumpfileDate = value;
                if (Loaded) Save();
            }
        }

        /// <summary>
        /// Date of last time that VNDB Stats were fetched.
        /// </summary>
        public DateTime StatsDate
        {
            get => _statsDate;
            set
            {
                if (_statsDate == value) return;
                _statsDate = value;
                if (Loaded) Save();
            }
        }

        /// <summary>
        /// Date of last time that User's User-related titles (URT) were fetched.
        /// </summary>
        public DateTime URTDate
        {
            get => _urtDate;
            set
            {
                if (_urtDate == value) return;
                _urtDate = value;
                if (Loaded) Save();
            }
        }

        /// <summary>
        /// Don't get titles released over a decade ago (does not apply to searched by name or favorite producer titles).
        /// </summary>
        public bool DecadeLimit
        {
            get => _decadeLimit;
            set
            {
                if (_decadeLimit == value) return;
                _decadeLimit = value;
                if (Loaded) Save();
            }
        }

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
    }
}
