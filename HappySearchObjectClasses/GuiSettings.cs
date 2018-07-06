using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Happy_Apps_Core
{
    public class GuiSettings : SettingsJsonFile
    {
        private bool _nsfwImages;
        private bool _advancedMode;
        private bool _contentTags;
        private bool _sexualTags;
        private bool _technicalTags;
        private string _culture;
        private int _maxClipboardSize;
	    private bool _captureClipboardOnStart;
	    private CultureInfo _cultureInfo = CultureInfo.DefaultThreadCurrentCulture;
	    private bool _googleUseCredential;

	    public GuiSettings()
        {
	        _maxClipboardSize = 700;
	        _culture = CultureInfo.CurrentCulture.ToString();
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
		public HashSet<int> AlertTraitIDs { get; } = new HashSet<int>();
    }

}