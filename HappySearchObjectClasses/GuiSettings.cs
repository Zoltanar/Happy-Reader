namespace Happy_Apps_Core
{
    public class GuiSettings
    {
        private bool _nsfwImages;
        private bool _advancedMode;
        private bool _contentTags;
        private bool _sexualTags;
        private bool _technicalTags;

        public bool NSFWImages
        {
            get => _nsfwImages;
            set
            {
                if (_nsfwImages == value) return;
                _nsfwImages = value;
                Save();
            }
        }

        public bool AdvancedMode
        {
            get => _advancedMode;
            set
            {
                if (_advancedMode == value) return;
                _advancedMode = value;
                Save();
            }
        }

        public bool ContentTags
        {
            get => _contentTags;
            set
            {
                if (_contentTags == value) return;
                _contentTags = value;
                Save();
            }
        }

        public bool SexualTags
        {
            get => _sexualTags;
            set
            {
                if (_sexualTags == value) return;
                _sexualTags = value;
                Save();
            }
        }

        public bool TechnicalTags
        {
            get => _technicalTags;
            set
            {
                if (_technicalTags == value) return;
                _technicalTags = value;
                Save();
            }
        }

        public static GuiSettings Load() => StaticHelpers.LoadJson<GuiSettings>(StaticHelpers.GuiSettingsJson);

        public void Save() => this.SaveJson(StaticHelpers.GuiSettingsJson);
    }
}