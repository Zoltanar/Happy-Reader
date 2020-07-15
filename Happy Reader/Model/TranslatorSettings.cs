using System.Collections.Generic;
using Happy_Apps_Core;

namespace Happy_Reader
{
	public class TranslatorSettings : SettingsJsonFile
	{
		private bool _googleUseCredential;
		private string _googleCredentialPath = "C:\\Google\\hrtranslate-credential.json";
		private string _freeUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

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

		public string GoogleCredentialPath
		{
			get => _googleCredentialPath;
			set
			{
				if (_googleCredentialPath == value) return;
				_googleCredentialPath = value;
				if (Loaded) Save();
			}
		}

		public string FreeUserAgent
		{
			get => _freeUserAgent;
			set
			{
				if (_freeUserAgent == value) return;
				_freeUserAgent = value;
				if (Loaded) Save();
			}
		}

		public HashSet<string> UntouchedStrings { get; set; } = new HashSet<string>{"","\r\n"};
	}
}