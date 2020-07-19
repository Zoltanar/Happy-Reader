using System;

namespace Happy_Apps_Core
{
	public class CoreSettings : SettingsJsonFile
	{
		private string _username = "guest";
		private int _userID = 0;
		private DateTime _dumpfileDate = DateTime.MinValue;

		//todo make editable
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

		//todo make editable
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
	}
}
