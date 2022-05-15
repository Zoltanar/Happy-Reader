using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Happy_Apps_Core;
using Happy_Reader.TranslationEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Happy_Reader
{
	public class JMDict
	{
		/// <summary>
		/// This list is ordered because when searching, we don't need to keep searching after strings have larger comparison value.
		/// </summary>
		private Term[] _dictionaryTerms = Array.Empty<Term>();
		private Dictionary<string, Kanji> _kanjiTerms = new();
		private static Dictionary<string, Tag> _termTags = new();
		private static Dictionary<string, Tag> _kanjiTags = new();

		public void ReadFiles(string folder)
		{
			_dictionaryTerms = Array.Empty<Term>();
			_kanjiTerms = new Dictionary<string, Kanji>();
			if (string.IsNullOrWhiteSpace(folder))
			{
				StaticHelpers.Logger.ToFile("Folder string was empty.");
				return;
			}
			var directory = new DirectoryInfo(folder);
			if (!directory.Exists)
			{
				StaticHelpers.Logger.ToFile($"Folder did not exist: {folder}");
				return;
			}
			StaticHelpers.Logger.ToFile("Reading files for JMDict and KanjiDic...");
			var loadingTerms = ReadJMDictFiles(Path.Combine(directory.FullName, "jmdict_english"), "term_bank_*json");
			var kanjiTerms = ReadKanjiDicFiles(Path.Combine(directory.FullName, "kanjidic_english"), "kanji_bank_*json");
			StaticHelpers.Logger.ToFile("Finished reading files for JMDict and KanjiDic.");
			_kanjiTerms = kanjiTerms.OrderBy(t => t.Character, StringComparer.Ordinal).ToDictionary(k => k.Character);
			_dictionaryTerms = loadingTerms.OrderBy(t => t.Expression, StringComparer.Ordinal).ToArray();
			StaticHelpers.Logger.ToFile($"JMDict: Loaded {_dictionaryTerms.Length:N0} terms and {_kanjiTerms.Count:N0} kanji.");
		}

		private List<Term> ReadJMDictFiles(string folder, string prefix)
		{
			if (!Directory.Exists(folder))
			{
				StaticHelpers.Logger.ToFile($"Folder did not exist: {folder}");
				return new List<Term>();
			}
			StaticHelpers.Logger.ToFile("Reading files for JMDict...");
			var loadingTerms = new List<Term>();
			foreach (var file in Directory.EnumerateFiles(folder, prefix))
			{
				try
				{
					var text = File.ReadAllText(file);
					var terms = JsonConvert.DeserializeObject<List<List<object>>>(text) ?? new List<List<object>>();
					loadingTerms.AddRange(terms.Select(t => new Term(t)));
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile($"Failed to read file for JMDict '{file}':", ex.ToString());
				}
			}
			_termTags = ReadTags(folder).ToDictionary(t => t.Name);
			return loadingTerms;
		}

		private List<Kanji> ReadKanjiDicFiles(string folder, string prefix)
		{
			if (!Directory.Exists(folder))
			{
				StaticHelpers.Logger.ToFile($"Folder did not exist: {folder}");
				return new List<Kanji>();
			}
			StaticHelpers.Logger.ToFile("Reading files for KanjiDic...");
			var kanjiTerms = new List<Kanji>();
			foreach (var file in Directory.EnumerateFiles(folder, prefix))
			{
				try
				{
					var text = File.ReadAllText(file);
					var terms = JsonConvert.DeserializeObject<List<List<object>>>(text) ?? new List<List<object>>();
					kanjiTerms.AddRange(terms.Select(t => new Kanji(t)));
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile($"Failed to read file for KanjiDic '{file}':", ex.ToString());
				}
			}

			_kanjiTags = ReadTags(folder).ToDictionary(t => t.Name);
			return kanjiTerms;
		}

		private List<Tag> ReadTags(string folder)
		{
			var list = new List<Tag>();
			foreach (var file in Directory.EnumerateFiles(folder, "tag_bank*json"))
			{
				try
				{
					var text = File.ReadAllText(file);
					var terms = JsonConvert.DeserializeObject<List<List<object>>>(text) ?? new List<List<object>>();
					list.AddRange(terms.Select(t => new Tag(t)));
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile($"Failed to read tag file '{file}':", ex.ToString());
				}
			}
			return list;
		}

		public List<ITerm> SearchExactTerm(string term)
		{
			var list = new List<Term>();
			int index = 0;
			while (index < _dictionaryTerms.Length)
			{
				var compared = string.Compare(_dictionaryTerms[index].Expression, term, StringComparison.Ordinal);
				if (compared == 0) list.Add(_dictionaryTerms[index]);
				if (compared > 0) break;
				index++;
			}
			if (list.Count == 0) return SearchKanji(term);
			return list.OrderByDescending(t => t.Score).Cast<ITerm>().ToList();
		}

		private List<ITerm> SearchKanji(string term)
		{
			return _kanjiTerms.TryGetValue(term.Substring(0,1), out var value) ? new List<ITerm> { value } : new List<ITerm>();
		}

		public List<ITerm> SearchTerm(string term)
		{
			var list = new List<Term>();
			int index = 0;
			while (index < _dictionaryTerms.Length)
            {
                if (term.StartsWith(_dictionaryTerms[index].Expression)) list.Add(_dictionaryTerms[index]);
				if (string.Compare(_dictionaryTerms[index].Expression, term, StringComparison.Ordinal) > 0) break;
				index++;
			}
			if (list.Count == 0) return SearchKanji(term);
			return list.OrderByDescending(t => t.Expression.Length).ThenByDescending(t => t.Score).Cast<ITerm>().ToList();
		}

		public bool SearchOuter(string text, out string result)
		{
			result = null;
			if (text.Length < 1 || Translator.LatinOnlyRegex.IsMatch(text)) return false;
			var results = SearchTerm(text);
			if (results.Count < 1) return false;
			result = string.Join(Environment.NewLine, results.Select(c => c.Detail(this)).Take(5));
			return true;
		}

		public interface ITerm
		{
			public string Detail(JMDict jmDict);
		}

		// ReSharper disable IdentifierTypo
		// ReSharper disable MemberCanBePrivate.Global
		// ReSharper disable UnusedAutoPropertyAccessor.Global
		public readonly struct Term : ITerm
		{
			public Term(List<object> list)
			{
				Expression = (string)list[0];
				Reading = (string)list[1];
				DefinitionTags = (string)list[2];
				Rules = (string)list[3];
				Score = (long)list[4];
				Glossary = ((JArray)list[5]).ToObject<string[]>();
				Sequence = (long)list[6];
				TermTags = (string)list[7];
			}
			public string Expression { get; }
			public string Reading { get; }
			public string DefinitionTags { get; }
			public string Rules { get; }
			public long Score { get; }
			public string[] Glossary { get; }
			public long Sequence { get; }
			public string TermTags { get; }

			public string Detail(JMDict jmDict)
			{
				var dReading = string.IsNullOrWhiteSpace(Reading)
					? string.Empty
					: $" ({Reading} {Translator.Instance.GetRomaji(Reading)})";
				string tags;
				if (StaticMethods.MainWindow.ViewModel.SettingsViewModel.TranslatorSettings.ShowTagsOnMouseover)
				{
					var dTags = jmDict.GetTags(DefinitionTags, false);
					var tTags = jmDict.GetTags(TermTags, false);
					tags = string.IsNullOrWhiteSpace(dTags) && string.IsNullOrWhiteSpace(tTags)
						? string.Empty
						: $"{dTags} {tTags}{Environment.NewLine}";
				}
				else tags = string.Empty;
				return $"{Expression}{dReading}{Environment.NewLine}{tags}{string.Join(", ", Glossary)}";
			}
		}

		private string GetTags(string tags, bool kanjiTags)
		{
			if (string.IsNullOrWhiteSpace(tags)) return string.Empty;
			var dict = kanjiTags ? _kanjiTags : _termTags;
			var tagParts = tags.Split(' ');
			var tagString = dict.TryGetValue(tagParts[0], out var tagValue) && !string.IsNullOrWhiteSpace(tagValue.Note) ? tagValue.Note : tagParts[0];
			for (var index = 1; index < tagParts.Length; index++)
			{
				tagString += " " + (dict.TryGetValue(tagParts[index], out var tagValueL) && !string.IsNullOrWhiteSpace(tagValueL.Note) ? tagValueL.Note : tagParts[index]);
			}
			return tagString;
		}

		public readonly struct Kanji : ITerm
		{
			public Kanji(List<object> list)
			{
				Character = (string)list[0];
				Onyomi = (string)list[1];
				Kunyomi = (string)list[2];
				Tags = (string)list[3];
				Meanings = ((JArray)list[4]).ToObject<string[]>();
				//stats = list[6];
			}
			public string Character { get; }
			public string Onyomi { get; }
			public string Kunyomi { get; }
			public string Tags { get; }
			public string[] Meanings { get; }
			//not used public string stats { get; }

			public string Detail(JMDict jmDict)
			{
				var jReading = $"{Onyomi} {Kunyomi}";
				var rReading = string.Join(" ", jReading.Split(' ').Select(r => Translator.Instance.GetRomaji(r).Replace(" ", "")));
				string tags;
				if (StaticMethods.MainWindow.ViewModel.SettingsViewModel.TranslatorSettings.ShowTagsOnMouseover)
				{
					var kTags = jmDict.GetTags(Tags, false);
					tags = string.IsNullOrWhiteSpace(kTags) ? string.Empty : $"{kTags}{Environment.NewLine}";
				}
				else tags = string.Empty;
				return $"{Character}{Environment.NewLine}{jReading}{Environment.NewLine}{rReading}{Environment.NewLine}{tags}{string.Join(", ", Meanings)}";
			}
		}

		public readonly struct Tag
		{
			public Tag(List<object> list)
			{
				Name = (string)list[0];
				Category = (string)list[1];
				Sort = (long)list[2];
				Note = (string)list[3];
				Score = (long)list[4];
			}

			public string Name { get; }
			public string Category { get; }
			public long Sort { get; }
			public string Note { get; }
			public long Score { get; }
		}
		// ReSharper restore UnusedAutoPropertyAccessor.Global
		// ReSharper restore MemberCanBePrivate.Global
		// ReSharper restore IdentifierTypo
	}
}
