using Happy_Apps_Core;

namespace Happy_Reader.ViewModel
{
	public class SettingsViewModel : SettingsJsonFile
	{
		public CoreSettings CoreSettings { get; set; }
		public GuiSettings GuiSettings { get; set; }
		public TranslatorSettings TranslatorSettings { get; set; }

		public SettingsViewModel()
		{
			CoreSettings = new CoreSettings();
			GuiSettings = new GuiSettings();
			TranslatorSettings = new TranslatorSettings();
		}
	}
}
