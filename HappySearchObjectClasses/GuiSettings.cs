using System.Collections.Generic;

namespace Happy_Apps_Core
{
    public class GuiSettings : SettingsJsonFile
    {
        private bool _nsfwImages;
        private bool _advancedMode;
        private bool _contentTags;
        private bool _sexualTags;
        private bool _technicalTags;
        private string _ithPath;
        private int _maxClipboardSize;
	    private bool _captureClipboardOnStart;
	    private bool _pauseIthVnrAndTranslate;

		public GuiSettings()
        {
            MaxClipboardSize = 700;
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

        public string IthPath
        {
            get => _ithPath;
            set
            {
                if (_ithPath == value) return;
                _ithPath = value;
                if (Loaded) Save();
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

	    public bool PauseIthVnrAndTranslate
	    {
		    get => _pauseIthVnrAndTranslate;
		    set
		    {
			    if (_pauseIthVnrAndTranslate == value) return;
			    _pauseIthVnrAndTranslate = value;
			    if (Loaded) Save();
		    }
	    }

		public HashSet<int> AlertTraitIDs { get; } = new HashSet<int>();
    }

}