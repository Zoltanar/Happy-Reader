using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using Happy_Apps_Core;
using Happy_Reader.View;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class GuiSettings : SettingsJsonFile
	{
		public const int VotesRequiredForRatingSort = 10; //todo make editable?
		private bool _nsfwImages;
		private bool _advancedMode;
		private bool _contentTags;
		private bool _sexualTags;
		private bool _technicalTags;
		private bool _hookGlobalMouse;
		private bool _useDecimalVoteScores = true;
		private bool _excludeLowVotesForRatingSort = true;
        private bool _displayGamesTab = true;
        private bool _displayHookTab = true;
        private bool _displayEntriesTab = true;
        private bool _displayTestTab = true;
        private bool _displayDatabaseTab = true;
        private bool _displayCharactersTab = true;
        private bool _displayProducersTab = true;
        private bool _displayInformationTab = true;
        private bool _displayApiLogTab = true;
        private bool _displayOtherLogsTab = true;
        private string _localeEmulatorPath;
		private string _culture;
		private GameLaunchMode _launchMode;
		private UserGameGrouping _userGameGrouping = UserGameGrouping.Added;
		private CultureInfo _cultureInfo = CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture;
		private List<PageLink> _pageLinks;
		private HashSet<string> _vnResolveExcludedNames = new(StringComparer.OrdinalIgnoreCase)
		{
			// ReSharper disable StringLiteralTypo
			"data",
			"windows-i686",
			"lib",
			"update",
			"install",
			"ihs",
			"lcsebody",
			"セーブデータフォルダを開く",
			"savedata",
			"cg",
			"patch",
			"plugin"
			// ReSharper restore StringLiteralTypo
		};
		
        [JsonIgnore]
		public CultureInfo[] Cultures { get; } = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);

		[JsonIgnore]
		public static ComboBoxItem[] LaunchModes { get; } = StaticMethods.GetEnumValues(typeof(GameLaunchMode));

		public GuiSettings()
		{
			_culture = CultureInfo.CurrentCulture.ToString();
			StaticMethods.ShowNSFWImages = () => NSFWImages;
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

		public bool HookGlobalMouse
		{
			get => _hookGlobalMouse;
			set
			{
				if (_hookGlobalMouse == value) return;
				_hookGlobalMouse = value;
				if (Loaded) Save();
			}
		}
		
		public bool UseDecimalVoteScores
		{
			get => _useDecimalVoteScores;
			set
			{
				if (_useDecimalVoteScores == value) return;
				_useDecimalVoteScores = value;
				if (Loaded) Save();
			}
		}
		
		public bool ExcludeLowVotesForRatingSort
		{
			get => _excludeLowVotesForRatingSort;
			set
			{
				if (_excludeLowVotesForRatingSort == value) return;
				_excludeLowVotesForRatingSort = value;
				if (Loaded) Save();
			}
		}

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

		public enum GameLaunchMode
		{
			[Description("Launch Normally")]
			Normal = 0,
			[Description("Launch Hooked Games with LE")]
			UseLeForHooked = 1,
			[Description("Launch All Games with LE")]
			UseLeForAll = 2,
		}
		
		public GameLaunchMode LaunchMode
		{
			get => _launchMode;
			set
			{
				if (_launchMode == value) return;
				_launchMode = value;
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

		//todo make editable
		public List<PageLink> PageLinks
		{
			get => _pageLinks;
			set
			{
				if (_pageLinks == value) return;
				_pageLinks = value;
				if (Loaded) Save();
			}
		}

		//todo make editable
		public HashSet<string> ExcludedNamesForVNResolve
		{
			get => _vnResolveExcludedNames;
			set
			{
				if (_vnResolveExcludedNames == value) return;
				_vnResolveExcludedNames = value;
				if (Loaded) Save();
			}
		}

		public UserGameGrouping UserGameGrouping
		{
			get => _userGameGrouping;
			set
			{
				if (_userGameGrouping == value) return;
				_userGameGrouping = value;
				if (Loaded) Save();
			}
		}

        public bool DisplayGamesTab
        {
            get => _displayGamesTab;
            set
            {
                if (_displayGamesTab == value) return;
                _displayGamesTab = value;
                if (Loaded) Save();
				OnPropertyChanged();
            }
        }
        public bool DisplayHookTab
        {
            get => _displayHookTab;
            set
            {
                if (_displayHookTab == value) return;
                _displayHookTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }
        public bool DisplayEntriesTab
        {
            get => _displayEntriesTab;
            set
            {
                if (_displayEntriesTab == value) return;
                _displayEntriesTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }
        public bool DisplayTestTab
        {
            get => _displayTestTab;
            set
            {
                if (_displayTestTab == value) return;
                _displayTestTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }
        public bool DisplayDatabaseTab
        {
            get => _displayDatabaseTab;
            set
            {
                if (_displayDatabaseTab == value) return;
                _displayDatabaseTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }
        public bool DisplayCharactersTab
        {
            get => _displayCharactersTab;
            set
            {
                if (_displayCharactersTab == value) return;
                _displayCharactersTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }
        public bool DisplayProducersTab
        {
            get => _displayProducersTab;
            set
            {
                if (_displayProducersTab == value) return;
                _displayProducersTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }
        public bool DisplayInformationTab
        {
            get => _displayInformationTab;
            set
            {
                if (_displayInformationTab == value) return;
                _displayInformationTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }
        public bool DisplayApiLogTab
        {
            get => _displayApiLogTab;
            set
            {
                if (_displayApiLogTab == value) return;
                _displayApiLogTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }
        public bool DisplayOtherLogsTab
        {
            get => _displayOtherLogsTab;
            set
            {
                if (_displayOtherLogsTab == value) return;
                _displayOtherLogsTab = value;
                if (Loaded) Save();
                OnPropertyChanged();
            }
        }

        public void SavePageLinks(IEnumerable<PageLink> pageLinks)
		{
			PageLinks = pageLinks.ToList();
		}

		public void SaveExcludedNamesForVNResolve(IEnumerable<string> excludedNamesForVnResolve)
		{
			ExcludedNamesForVNResolve = excludedNamesForVnResolve.ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		public bool ShowTags(Happy_Apps_Core.Database.DbTag.TagCategory category)
		{
			return category switch
			{
				Happy_Apps_Core.Database.DbTag.TagCategory.Content => ContentTags,
				Happy_Apps_Core.Database.DbTag.TagCategory.Sexual => SexualTags,
				Happy_Apps_Core.Database.DbTag.TagCategory.Technical => TechnicalTags,
				_ => throw new ArgumentOutOfRangeException()
			};
		}
	}
}