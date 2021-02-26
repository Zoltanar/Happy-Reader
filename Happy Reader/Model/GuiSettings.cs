using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Happy_Apps_Core;
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
		private bool _hookGlobalMouse;
		private bool _hookIthVnr;
		private bool _excludeLowVotesForRatingSort = true;
		private string _localeEmulatorPath;
		private string _culture;
		private CultureInfo _cultureInfo = CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture;
		private List<PageLink> _pageLinks;

		[JsonIgnore]
		public string About => $"{StaticHelpers.ClientName} {StaticHelpers.ClientVersion}";

		[JsonIgnore]
		public CultureInfo[] Cultures { get; } = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);

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

		public bool HookIthVnr
		{
			get => _hookIthVnr;
			set
			{
				if (_hookIthVnr == value) return;
				_hookIthVnr = value;
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
		
		public void SavePageLinks(IEnumerable<PageLink> pageLinks)
		{
			PageLinks = pageLinks.ToList();
		}
	}
}