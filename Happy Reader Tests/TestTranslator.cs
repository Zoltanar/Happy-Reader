using System;
using System.Collections.Generic;
using Happy_Apps_Core.Translation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Happy_Reader_Tests
{
	class TestTranslator : ITranslator
	{
		public string Version => "v1.0";
		public string SourceName => "Unit Test Translator";
		public IReadOnlyDictionary<string, Type> Properties => throw new NotSupportedException();
		public string Error { get; set; }

		public Dictionary<string, string> Translations = new Dictionary<string, string>()
		{
			{"私は本田です。", "I am Honda."},
			{"私は本田ですそれともあなたは本田。","I am Honda and you are Honda."},
			{"私は本田ですそれともあなたは武田。", "I am Honda and you are Takeda."}
		};

		public void Initialise()
		{
			//ignore
		}

		public void LoadProperties(string filePath)
		{
			//ignore
		}

		public void SaveProperties(string filePath)
		{
			throw new NotSupportedException();
		}

		public void SetProperty(string propertyName, object value)
		{
			throw new NotSupportedException();
		}

		public object GetProperty(string propertyName)
		{
			throw new NotSupportedException();
		}

		public bool Translate(string input, out string output)
		{
			if (!Translations.TryGetValue(input, out output)) Assert.Inconclusive($"Input not added to Test Dictionary: {input}");
			return true;
		}
	}
}
