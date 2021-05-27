using Happy_Apps_Core;
using Newtonsoft.Json;

namespace Happy_Reader.ViewModel
{
	public class SettingsViewModel : SettingsJsonFile
	{
		[JsonIgnore]
		public override bool Loaded
		{
			set
			{
				base.Loaded = value;
				CoreSettings.Loaded = true;
				GuiSettings.Loaded = true;
				TranslatorSettings.Loaded = true;
			}
			get => base.Loaded;
		}

		[JsonIgnore]
		public override string FilePath
		{
			set
			{
				base.FilePath = value;
				CoreSettings.FilePath = value;
				GuiSettings.FilePath = value;
				TranslatorSettings.FilePath = value;
			}
			get => base.FilePath;
		}

		public CoreSettings CoreSettings { get; set; }
		public GuiSettings GuiSettings { get; set; }
		public TranslatorSettings TranslatorSettings { get; set; }
		
		public SettingsViewModel()
		{
			CoreSettings = new CoreSettings { ObjectToSerialise = this };
			GuiSettings = new GuiSettings { ObjectToSerialise = this };
			TranslatorSettings = new TranslatorSettings { ObjectToSerialise = this };
		}
	}
}
