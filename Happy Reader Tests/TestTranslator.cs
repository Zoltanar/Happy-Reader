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
			{"私は山澤です。", "I am Yamazawa."},
			{"私は本田ですそれともあなたは本田。","I am Honda and you are Honda."},
			{"私は武田ですそれともあなたは本田。","I am Takeda and you are Honda."},
			{"私は本田ですそれともあなたは武田。", "I am Honda and you are Takeda."},
			{"私は山澤ですそれともあなたは本田。", "I am Yamazawa and you are Honda."},
			{"私は山澤ですそれともあなたは武田。", "I am Yamazawa and you are Takeda."},
			{"私は武田ですそれともあなたは山澤。", "I am Takeda and you are Yamazawa."},
			{"私は由紀子ですそれともあなたは本田。","I am Yukiko and you are Honda."},
			{"私は武田ですそれともあなたは山澤、最初は彼は本田。", "I am Takeda and you are Yamazawa, finally, he is Honda."},
			{"本田できたそしてもお腹すいたみたい。", "Honda came and he looked hungry."},
			{"由紀子できたそしてもお腹すいたみたい。", "Yukiko came and she looked hungry."}
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
