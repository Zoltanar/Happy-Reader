using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Happy_Reader.Database;
using Kawazu;

namespace Happy_Reader.TranslationEngine
{
	public partial class Translator
	{
		private static readonly KawazuConverter KawazuConverter = new();
		public static readonly IReadOnlyDictionary<string, Func<string, string>> RomajiTranslators = new ReadOnlyDictionary<string, Func<string, string>>(
			new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
			{
				{ "Kawazu", KawazuToRomaji },
				{ "Kakasi", Kakasi.JapaneseToRomaji },
			});

		private string RomajiTranslator => _settings.SelectedRomajiTranslator;

		public void GetRomajiFiltered(StringBuilder text, TranslationResults result)
		{
			ReplacePreRomaji(text, result);
			var parts = new List<(string Part, bool Translate)>();
			SplitInputIntoParts(text.ToString(), parts);
			text.Clear();
			foreach (var part in parts) text.Append(part.Translate ? GetRomaji(part.Part) : part.Part);
			ReplacePostRomaji(text, result);
		}

		public string GetRomaji(string text)
		{
			return RomajiTranslators[RomajiTranslator](text);
		}

		private void ReplacePreRomaji(StringBuilder sb, TranslationResults result)
		{
			var entries = OrderEntries(_entries.Where(x => x.Type == EntryType.Name || x.Type == EntryType.Yomi || x.Type == EntryType.PreRomaji)).ToArray();
			var usefulEntries = RemoveUnusedEntriesAndSetLocation(sb, entries);
			MergeNeighbouringEntries(usefulEntries);
			foreach (var entry in usefulEntries)
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, result);
				else LogReplace(sb, entry, result);
			}
		}

		private void ReplacePostRomaji(StringBuilder sb, TranslationResults result)
		{
			foreach (var entry in OrderEntries(_entries.Where(x => x.Type == EntryType.PostRomaji)))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, result);
				else LogReplace(sb, entry, result);
			}
		}

		private static string KawazuToRomaji(string text)
		{
			var result = Task.Run(() => KawazuConvert(text)).GetAwaiter().GetResult();
			result = result.Replace('ゔ', 'v');
			result = result.Replace("mp", "np");
			result = result.Replace("mb", "nb");
			return result;
		}

		private static async Task<string> KawazuConvert(string text)
		{
			try
			{
				return await KawazuConverter.Convert(text, To.Romaji, Mode.Spaced, RomajiSystem.Hepburn);
			}
			catch (Exception ex)
			{
				return $"Failed: {ex.Message}";
			}
		}
	}
}
