using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class GuiSettings : SettingsJsonFile
	{
		private bool _nsfwImages;
		private bool _advancedMode;
		private bool _contentTags;
		private bool _sexualTags;
		private bool _technicalTags;
		private string _culture;
		private CultureInfo _cultureInfo = CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture;
		private string _localeEmulatorPath;
		private string _extraPageLink;

		[JsonIgnore]
		public CultureInfo[] Cultures { get; } = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);

		public GuiSettings()
		{
			_culture = CultureInfo.CurrentCulture.ToString();
			ListedVN.ShowNSFWImages = () => NSFWImages;
		}

		public bool NSFWImages
		{
			get => _nsfwImages;
			set
			{
				if (_nsfwImages == value) return;
				_nsfwImages = value;
				if (Loaded) Save();
			}
		}

		/// <summary>
		/// Whether API queries/responses should be logged.
		/// </summary>
		public bool AdvancedMode
		{
			get => _advancedMode;
			set
			{
				if (_advancedMode == value) return;
				_advancedMode = value;
				if (Loaded) Save();
			}
		}

		public bool ContentTags
		{
			get => _contentTags;
			set
			{
				if (_contentTags == value) return;
				_contentTags = value;
				if (Loaded) Save();
			}
		}

		public bool SexualTags
		{
			get => _sexualTags;
			set
			{
				if (_sexualTags == value) return;
				_sexualTags = value;
				if (Loaded) Save();
			}
		}

		public bool TechnicalTags
		{
			get => _technicalTags;
			set
			{
				if (_technicalTags == value) return;
				_technicalTags = value;
				if (Loaded) Save();
			}
		}

		public string Culture
		{
			get => _culture;
			set
			{
				if (_culture == value || value == null) return;
				_culture = value;
				try
				{
					CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(_culture);
				}
				catch (CultureNotFoundException)
				{
					CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture;
				}
				CultureInfo = CultureInfo.DefaultThreadCurrentUICulture;
				if (Loaded) Save();
			}
		}

		[JsonIgnore]
		public CultureInfo CultureInfo
		{
			get => _cultureInfo;
			set
			{
				if (Equals(_cultureInfo, value)) return;
				_cultureInfo = value;
				Culture = _cultureInfo.ToString();
			}
		}

		public string About =>
			$"{StaticHelpers.ClientName} {StaticHelpers.ClientVersion} for VNDB API version {StaticHelpers.APIVersion}";

		//todo make editable
		public List<int> AlertTagIDs { get; } = new List<int>();

		//todo make editable
		public List<int> AlertTraitIDs { get; } = new List<int>();

		//todo make editable
		public List<double> AlertTagValues { get; } = new List<double>();

		//todo make editable
		public List<double> AlertTraitValues { get; } = new List<double>();

		public string LocaleEmulatorPath
		{
			get => _localeEmulatorPath;
			set
			{
				if (_localeEmulatorPath == value) return;
				_localeEmulatorPath = value;
				if (Loaded) Save();
			}
		}

		public string ExtraPageLink
		{
			get => _extraPageLink;
			set
			{
				if (_extraPageLink == value) return;
				_extraPageLink = value;
				if (Loaded) Save();
			}
		}
		
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