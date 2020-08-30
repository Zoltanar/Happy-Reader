using System;
using System.Collections.Generic;

namespace Happy_Apps_Core
{
	public class CoreSettings : SettingsJsonFile
	{
		private string _username = "guest";
		private int _userID;
		private DateTime _dumpfileDate = DateTime.MinValue;

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

		//todo make editable
		public List<int> AlertTagIDs { get; } = new List<int>();

		//todo make editable
		public List<int> AlertTraitIDs { get; } = new List<int>();

		//todo make editable
		public List<double> AlertTagValues { get; } = new List<double>();

		//todo make editable
		public List<double> AlertTraitValues { get; } = new List<double>();

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
