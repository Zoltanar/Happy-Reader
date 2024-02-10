using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Translation;
using Happy_Reader.Database;
using Happy_Reader.TranslationEngine;

namespace Happy_Reader
{
	public class TranslationResults
	{
		public string[] Text { get; } = new string[8];
        public List<Entry>[] EntriesUsed { get; }
        public List<CachedTranslation> TranslationsUsed { get; }
        public bool SaveData { get; }
        public List<ProxiesWithCount> ProxiesUsed { get; } = new();

        private int _currentStage;


		public TranslationResults(bool saveDataUsed)
		{
            SaveData = saveDataUsed;
			EntriesUsed = Enumerable.Range(0, 8).Select(_ => new List<Entry>()).ToArray();
			TranslationsUsed = new List<CachedTranslation>();
		}

		public TranslationResults(string part)
		{
			Text = Enumerable.Repeat(part, 8).ToArray();
		}

		public string this[int index]
		{
			get => Text[index];
			set => Text[index] = value;
		}

        public void AddProxiesUsed(IEnumerable<ProxiesWithCount> proxiesUsed)
        {
            ProxiesUsed.AddRange(proxiesUsed);
        }

		public void AddEntryUsed(Entry entry)
		{
			EntriesUsed[_currentStage].Add(entry);
		}
		public void AddTranslationUsed(CachedTranslation translation) => TranslationsUsed.Add(translation);

		public void SetStage(int stage)
		{
			if (stage < _currentStage || stage - _currentStage != 1)
			{
				//shouldn't happen, break here.
			}
			_currentStage = stage;
		}
	}
}