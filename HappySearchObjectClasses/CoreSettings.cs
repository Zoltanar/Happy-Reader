using System;
using System.Collections.Generic;
using System.IO;

namespace Happy_Apps_Core
{
	public class CoreSettings : SettingsJsonFile
	{
		private string _username = "guest";
		private int _userID;
		private DateTime _dumpfileDate = DateTime.MinValue;
		private bool _clearOldDumpsAndBackups = true;
		private string _imageFolderPath = Path.Combine(StaticHelpers.StoredDataFolder, "vndb-img\\");
		private ImageSyncMode _imageSync = ImageSyncMode.None;
        private string _secondaryTitleLanguage = "en";
        private string _apiToken;

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
        /// API Token for VNDB User
        /// </summary>
        public string ApiToken
        {
            get => _apiToken;
            set
            {
                if (_apiToken == value) return;
                _apiToken = value;
                if (Loaded) Save();
            }
        }

        /// <summary>
        /// Date of last time that tag/trait dump files were downloaded.
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
		/// Delete old database dump files and database backups, when updating.
		/// </summary>
		public bool ClearOldDumpsAndBackups
		{
			get => _clearOldDumpsAndBackups;
			set
			{
				if (_clearOldDumpsAndBackups == value) return;
				_clearOldDumpsAndBackups = value;
				if (Loaded) Save();
			}
		}

		/// <summary>
		/// Path to folder containing images for VNDB data.
		/// </summary>
		public string ImageFolderPath
		{
			get => _imageFolderPath;
			set
			{
				if (_imageFolderPath == value) return;
				_imageFolderPath = value;
				if (Loaded) Save();
			}
		}

		/// <summary>
		/// Mode for downloading/updating images when updating database.
		/// </summary>
		public ImageSyncMode SyncImages
		{
			get => _imageSync;
			set
			{
				if (_imageSync == value) return;
				_imageSync = value;
				if (Loaded) Save();
			}
        }

        public string SecondaryTitleLanguage
        {
            get => _secondaryTitleLanguage;
            set
            {
                if (_secondaryTitleLanguage == value) return;
                _secondaryTitleLanguage = value;
                if (Loaded) Save();
            }
        }

        //todo make editable
        public List<int> AlertTagIDs { get; set;  } = new();

		//todo make editable
		public List<int> AlertTraitIDs { get; set; } = new();

		//todo make editable
		public List<double> AlertTagValues { get; set; } = new();

		//todo make editable
		public List<double> AlertTraitValues { get; set; } = new();

		public Dictionary<DumpFiles.WrittenTag, double> GetTagScoreDictionary()
		{
			var tagScoreDict = new Dictionary<DumpFiles.WrittenTag, double>();
			for (var index = 0; index < AlertTagIDs.Count; index++)
			{
				tagScoreDict.Add(DumpFiles.GetTag(AlertTagIDs[index]), AlertTagValues[index]);
			}
			return tagScoreDict;
		}

		public Dictionary<DumpFiles.WrittenTrait, double> GetTraitScoreDictionary()
		{
			var traitScoreDict = new Dictionary<DumpFiles.WrittenTrait, double>();
			for (var index = 0; index < AlertTraitIDs.Count; index++)
			{
				traitScoreDict.Add(DumpFiles.GetTrait(AlertTraitIDs[index]), AlertTraitValues[index]);
			}
			return traitScoreDict;
		}
	}
}
