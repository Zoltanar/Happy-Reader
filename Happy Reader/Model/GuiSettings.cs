using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class GuiSettings : SettingsJsonFile, INotifyPropertyChanged
	{
		private bool _nsfwImages;
		private bool _advancedMode;
		private bool _contentTags;
		private bool _sexualTags;
		private bool _technicalTags;
		private string _culture;
		private int _maxClipboardSize;
		private bool _captureClipboardOnStart;
		private CultureInfo _cultureInfo = CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture;
		private bool _googleUseCredential;
		private string _originalTextFont;
		private string _romajiTextFont;
		private string _translatedTextFont;
		private string _localeEmulatorPath;
		private string _extraPageLink;

		public CultureInfo[] Cultures { get; } = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);

		public GuiSettings()
		{
			_maxClipboardSize = 700;
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

		public int MaxClipboardSize
		{
			get => _maxClipboardSize;
			set
			{
				if (_maxClipboardSize == value) return;
				_maxClipboardSize = value;
				if (Loaded) Save();
			}
		}

		public bool CaptureClipboardOnStart
		{
			get => _captureClipboardOnStart;
			set
			{
				if (_captureClipboardOnStart == value) return;
				_captureClipboardOnStart = value;
				if (Loaded) Save();
			}
		}

		public bool GoogleUseCredential
		{
			get => _googleUseCredential;
			set
			{
				if (_googleUseCredential == value) return;
				_googleUseCredential = value;
				if (Loaded) Save();
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

		[JsonIgnore]
		public BindingList<string> VndbQueries { get; set; }
		[JsonIgnore]
		public BindingList<string> VndbResponses { get; set; }

		public string OriginalTextFont
		{
			get => _originalTextFont;
			set
			{
				if (_originalTextFont == value) return;
				if (!string.IsNullOrWhiteSpace(value) && !StaticMethods.FontsInstalled.ContainsKey(value))
				{
					StaticHelpers.Logger.ToFile($"Did not find font with name '{value}'");
				}
				_originalTextFont = value;
				if (Loaded) Save();
			}
		}

		public string RomajiTextFont
		{
			get => _romajiTextFont;
			set
			{
				if (_romajiTextFont == value) return;
				if (!string.IsNullOrWhiteSpace(value) && !StaticMethods.FontsInstalled.ContainsKey(value))
				{
					StaticHelpers.Logger.ToFile($"Did not find font with name '{value}'");
				}
				_romajiTextFont = value;
				if (Loaded) Save();
			}
		}

		public string TranslatedTextFont
		{
			get => _translatedTextFont;
			set
			{
				if (_translatedTextFont == value) return;
				if (!string.IsNullOrWhiteSpace(value) && !StaticMethods.FontsInstalled.ContainsKey(value))
				{
					StaticHelpers.Logger.ToFile($"Did not find font with name '{value}'");
				}
				_translatedTextFont = value;
				if (Loaded) Save();
			}
		}

		[JsonIgnore]
		public Brush OriginalColor => Brushes.Ivory;
		[JsonIgnore]
		public Brush RomajiColor => Brushes.Pink;
		[JsonIgnore]
		public Brush TranslationColor => Brushes.GreenYellow;
		[JsonIgnore]
		public double FontSize => 22d;

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

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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