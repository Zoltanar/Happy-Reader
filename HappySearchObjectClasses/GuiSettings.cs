namespace Happy_Apps_Core
{
    public class GuiSettings
    {
        private bool _nsfwImages;
        private bool _advancedMode;

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

        public static GuiSettings Load() => StaticHelpers.LoadJson<GuiSettings>(StaticHelpers.GuiSettingsJson);

        public void Save() => this.SaveJson(StaticHelpers.GuiSettingsJson);
    }
}