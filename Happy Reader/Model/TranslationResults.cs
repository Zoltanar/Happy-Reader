using System.Collections.Generic;
using System.Linq;
using Happy_Reader.Database;

namespace Happy_Reader
{
	public class TranslationResults
	{
		public string[] Text { get; } = new string[8];
		public List<Entry>[] EntriesUsed { get; }
		public bool SaveEntries { get; }
		private int _currentStage;


		public TranslationResults(bool saveEntriesUsed)
		{
			SaveEntries = saveEntriesUsed;
			EntriesUsed = Enumerable.Range(0, 8).Select(_ => new List<Entry>()).ToArray();
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

		public void AddEntryUsed(Entry entry)
		{
			EntriesUsed[_currentStage].Add(entry);
		}

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