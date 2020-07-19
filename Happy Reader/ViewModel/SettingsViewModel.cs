using Happy_Apps_Core;

namespace Happy_Reader.ViewModel
{
	public class SettingsViewModel
	{
		public CoreSettings CoreSettings { get; }
		public GuiSettings GuiSettings { get; }
		public TranslatorSettings TranslatorSettings { get; }

		public SettingsViewModel(CoreSettings coreSettings, GuiSettings guiSettings, TranslatorSettings translatorSettings)
		{
			CoreSettings = coreSettings;
			GuiSettings = guiSettings;
			TranslatorSettings = translatorSettings;
		}
	}
}
