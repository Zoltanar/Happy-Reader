using Happy_Apps_Core;

namespace DatabaseDumpReader
{
	public class SettingsViewModel : SettingsJsonFile
	{
		public CoreSettings CoreSettings { get; set; }
		public object GuiSettings { get; set; }
		public object TranslatorSettings { get; set; }
	}
}
