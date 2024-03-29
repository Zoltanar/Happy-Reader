﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Apps_Core.Translation;
using Happy_Reader.Database;
using JetBrains.Annotations;

namespace Happy_Reader.TranslationEngine
{
	public partial class Translator
	{
		private static readonly object TranslateLock = new();
		private static readonly Dictionary<string, Regex> RegexDict = new();
		private static readonly Regex Stage4P1InputRegex = new(@"\[\[([^];]+?)]]", RegexOptions.Compiled);
		private static readonly Regex Stage4P1OutputRegex = new(@"^.*?\[\[([^];]+)]].*?$", RegexOptions.Compiled);
		private static readonly Regex RemoveNewLineRegex = new(@"[\r\n]", RegexOptions.Compiled);
		public static readonly Regex LatinOnlyRegex = new(@"^[a-zA-Z0-9:+|\-[\]\/\\\r\n\s.!?,;@()_$^""]+$", RegexOptions.Compiled);
		private readonly HappyReaderDatabase _data;
		private readonly TranslatorSettings _settings;
		private User _lastUser;
		private EntryGame _lastGame = EntryGame.None;
        private Entry[] _entries;
        private bool _logVerbose;
		private char[] _inclusiveSeparators = { };
		private char[] _allSeparators = { };
		private static bool _useAnyCached = true; //todo keep cache collection grouped by source and change primary key to input + source
		private static uint GotFromCacheCount { get; set; }
		private static uint GotFromApiCount { get; set; }
		public bool RefreshEntries = true;
		private ITranslator SelectedTranslator => _settings.SelectedTranslator;
		public static Translator Instance { get; set; }
		public JMDict OfflineDictionary { get; } = new();

		public Translator(HappyReaderDatabase data, TranslatorSettings settings)
		{
			_data = data;
			_settings = settings;
			_settings.UpdateOfflineDictionaryFolder = UpdateOfflineDictionaryFolder;
		}

		public void Initialise(bool logVerbose)
		{
			_inclusiveSeparators = _settings.InclusiveSeparators.ToCharArray();
			_allSeparators = _settings.ExclusiveSeparators.Concat(_settings.InclusiveSeparators).ToArray();
			_logVerbose = logVerbose;
			UpdateOfflineDictionaryFolder();
        }

		private void UpdateOfflineDictionaryFolder()
		{
			OfflineDictionary.ReadFiles(_settings.OfflineDictionaryFolder);
		}

		public Translation Translate(User user, EntryGame game, string input, bool saveDataUsed, bool removeRepetition)
		{
			if (removeRepetition)
			{
				int loopCount = 0;
				while (true)
				{
					if (loopCount > 1000) throw new ArgumentException($"Loop executed {loopCount} times, likely to be stuck.");
					var before = input;
					input = ReduceRepeatedString(before);
					if (input == before) break;
					loopCount++;
				}
			}
			input = RemoveNewLineRegex.Replace(input, "");
			if (string.IsNullOrWhiteSpace(input))
			{
				return Translation.Error("Input was empty.");
			}
			if (input.Length > _settings.MaxOutputSize)
			{
				return Translation.Error($"Exceeded maximum output size ({input.Length}/{_settings.MaxOutputSize})");
			}
			lock (TranslateLock)
			{
				if (user != _lastUser || !game.Equals(_lastGame) || RefreshEntries) SetEntries(user, game);
				if (LatinOnlyRegex.IsMatch(input)) return Translation.Error("Input was Latin only.");
				var item = new Translation(input, true);
				SplitInputIntoParts(item.Original, item.Parts,false);
				item.TranslateParts(saveDataUsed);
				return item;
			}
		}

		public TranslationResults TranslatePart(string input, bool saveDataUsed)
		{
			var result = new TranslationResults(saveDataUsed);
			var sb = new StringBuilder(input);
			if (TranslateSingleKana(sb, input))
			{
				result[0] = input;
				result[1] = input;
				result[2] = input;
				result[3] = input;
				result[4] = input;
				result[5] = sb.ToString();
				result[6] = sb.ToString();
				result[7] = sb.ToString();
				return result;
			}
			StaticHelpers.Logger.Verbose($"Stage 0: {sb}");
			result[0] = sb.ToString();
			result.SetStage(1);
			result[1] = sb.ToString();
			TranslateStageTwo(sb, result);
			TranslateStageThree(sb, result);
			List<Entry> usefulEntriesWithProxies;
			try
			{
				usefulEntriesWithProxies = TranslateStageFour(sb, _data, result);
			}
			catch (Exception ex)
			{
				result[4] = ex.Message;
				result[5] = ex.Message;
				result[6] = ex.Message;
				result[7] = ex.Message;
				return result;
			}
			var singleEntry = usefulEntriesWithProxies.Select(e => e.AssignedProxy.Entry).FirstOrDefault(e => e.Input == sb.ToString());
			if (singleEntry != null)
			{
				sb.Clear();
				sb.Append(singleEntry.Output);
                result.SetStage(5);
                result[5] = sb.ToString();
            }
			else if (sb.ToString().All(c => _allSeparators.Contains(c)))
            {
                result.SetStage(5);
                result[5] = sb.ToString();
            }
			else TranslateStageFive(sb, result);
			TranslateStageSix(sb, usefulEntriesWithProxies, result);
			TranslateStageSeven(sb, result);
			Capitalise(sb, result);
			return result;
		}

		private void Capitalise(StringBuilder sb, TranslationResults result)
		{
			int i = 0;
			while (i < sb.Length)
			{
				if (char.IsLetter(sb[i]))
				{
					sb[i] = char.ToUpperInvariant(sb[i]);
					result[7] = sb.ToString();
					return;
				}
				i++;
			}
		}


		/// <summary>
		/// If start of input is repeated, it is removed.
		/// </summary>
		/// <param name="input">String to be reduced.</param>
		/// <returns>String without repeated segment at the start.</returns>
		private static string ReduceRepeatedString(string input)
		{
			if (input.Length == 1) return input;
			var firstChar = input[0];
			var indexOfSecondBracket = input.IndexOf(firstChar, 1);
			if (indexOfSecondBracket == -1) return input;
			int skip = 0;
			while (indexOfSecondBracket + skip < input.Length && input[skip] == input[indexOfSecondBracket + skip] && skip < indexOfSecondBracket) skip++;
			return skip != indexOfSecondBracket ? input : input.Substring(skip);
		}

		private void SplitInputIntoParts(string input, ICollection<(string Part, bool Translate)> parts, bool forRomaji)
		{
			int index = 0;
			string currentPart = "";
			while (index < input.Length)
			{
				var @char = input[index];
				if (_allSeparators.Contains(@char))
				{
					if (_inclusiveSeparators.Contains(@char))
					{
						currentPart += @char;
						var translatePart = !currentPart.All(c => _allSeparators.Contains(c)) && !LatinOnlyRegex.IsMatch(currentPart);
						parts.Add((currentPart, translatePart && (_settings.OutputTranslation || forRomaji)));
					}
					else
					{
						if (currentPart.Length > 0)
						{
							//not empty, not all separators, not latin only
							var translatePart = !string.IsNullOrWhiteSpace(currentPart) && !currentPart.All(c => _allSeparators.Contains(c)) && !LatinOnlyRegex.IsMatch(currentPart);
							parts.Add((currentPart, translatePart && (_settings.OutputTranslation || forRomaji)));
						}
						parts.Add((@char.ToString(), false));
					}
					currentPart = "";
					index++;
					continue;
				}
				currentPart += @char;
				index++;
			}
			if (currentPart.Length > 0)
			{
				var translatePart = !string.IsNullOrWhiteSpace(currentPart) && !currentPart.All(c => _allSeparators.Contains(c)) && !LatinOnlyRegex.IsMatch(currentPart);
				parts.Add((currentPart, translatePart && (_settings.OutputTranslation || forRomaji)));
			}
		}

		private void SetEntries([NotNull] User user, EntryGame game)
		{
			RefreshEntries = false;
			_lastUser = user;
			_lastGame = game;
			var gamesInSeries = HappyReaderDatabase.GetSeriesEntryGames(game);
			var generalEntries = _data.Entries.Where(e => (e.Private && e.UserId == user.Id || !e.Private) && !e.SeriesSpecific).ToArray();
			Entry[] specificEntries = { };
			if (gamesInSeries.Length > 0)
			{
				specificEntries = _data.Entries.Where(e => (e.Private && e.UserId == user.Id || !e.Private)
																									 && e.SeriesSpecific
																									 && (e.GameData?.GameId.HasValue ?? false)
																									 && gamesInSeries.Contains(e.GameData)).ToArray();
			}
			_entries = generalEntries.Concat(specificEntries).Where(e => !e.Disabled).OrderBy(i => i.Id).ToArray();
			StaticHelpers.Logger.ToDebug($"[Translator] General entries: {generalEntries.Length}. Specific entries: {specificEntries.Length}");
		}

		/// <summary>
		/// Replace entries of type Game.
		/// </summary>
		public void TranslateStageOne(StringBuilder sb, TranslationResults result)
		{
			result?.SetStage(1);
			foreach (var entry in OrderEntries(_entries.Where(i => i.Type == EntryType.Game)))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, result);
				else LogReplace(sb, entry, result, false);
			}
			StaticHelpers.Logger.Verbose($"Stage 1: {sb}");
			//Stage One is also used for Translation.Original which does not require setting result to array.
			if (result != null) result[1] = sb.ToString();
		}

		/// <summary>
		/// Replace entries of type Input.
		/// </summary>
		private void TranslateStageTwo(StringBuilder sb, TranslationResults result)
		{
			result.SetStage(2);
			foreach (var entry in OrderEntries(_entries.Where(i => i.Type == EntryType.Input)))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, result);
				else LogReplace(sb, entry, result, false);
			}
			StaticHelpers.Logger.Verbose($"Stage 2: {sb}");
			result[2] = sb.ToString();
		}

		/// <summary>
		/// Replace entries of type Yomi.
		/// </summary>
		private void TranslateStageThree(StringBuilder sb, TranslationResults result)
		{
			result.SetStage(3);
			foreach (var entry in OrderEntries(_entries.Where(i => i.Type == EntryType.Yomi)))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, result);
				else LogReplace(sb, entry, result, false);
			}
			StaticHelpers.Logger.Verbose($"Stage 3: {sb}");
			result[3] = sb.ToString();
		}

		/*TranslateStageFour in Proxies file: Replace entries of type Name and Translation to proxies.*/

		/// <summary>
		/// Machine translation stage.
		/// </summary>
		private void TranslateStageFive(StringBuilder sb, TranslationResults result)
		{
			result.SetStage(5);
            MachineTranslate(sb, result);
			StaticHelpers.Logger.Verbose($"Stage 5: {sb}");
			result[5] = sb.ToString();
		}

		/*TranslateStageSix in Proxies file: Replace Name and Translation proxies to entry outputs.*/

		/// <summary>
		/// Replace entries of type Output.
		/// </summary>
		private void TranslateStageSeven(StringBuilder sb, TranslationResults result)
		{
			result.SetStage(7);
			foreach (var entry in OrderEntries(_entries.Where(i => i.Type == EntryType.Output)))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry.Input, entry.Output, result, entry);
				else LogReplace(sb, entry.Input, entry.Output, result, entry, false);
			}
			StaticHelpers.Logger.Verbose($"Stage 7: {sb}");
			result[7] = sb.ToString();
		}

		private static Regex GetRegex(string input)
		{
			if (!RegexDict.TryGetValue(input, out var regex))
			{
				regex = RegexDict[input] = new Regex(input);
			}
			return regex;
		}

		private static IEnumerable<Entry> OrderEntries(IEnumerable<Entry> entries)
		{
			//todo add priority
			return entries.OrderByDescending(x => x.Input.Length);
		}

		private static List<Entry> RemoveUnusedEntriesAndSetLocation(StringBuilder sb, IList<Entry> entries)
		{
			var text = sb.ToString();
			List<Entry> relevantEntries = new List<Entry>();
			foreach (var entry in entries)
			{
				int? location = null;
				if (entry.Regex)
				{
					var match = GetRegex(entry.Input).Match(text);
					if (match.Success) location = match.Index;
				}
				else
				{
					var indexOfEntry = text.IndexOf(entry.Input, StringComparison.Ordinal);
					if (indexOfEntry >= 0) location = indexOfEntry;
				}
				//remove unused entries or set location
				if (location.HasValue)
				{
					var existing = relevantEntries.FirstOrDefault(e => e.Input.StartsWith(entry.Input));
					if (existing == null || existing.Location != location.Value)
					{
						entry.Location = location.Value;
						relevantEntries.Add(entry);
					}
				}
			}
			return relevantEntries.OrderBy(x => x.Location).ToList();
		}

		private static void MergeNeighbouringEntries(IList<Entry> entries)
		{
			var entriesToRemove = new List<Entry>();
			var entriesToAdd = new List<Entry>();
			for (var index = 1; index < entries.Count; index++)
			{
				var previousEntry = entries[index - 1];
				var entry = entries[index];
				if (previousEntry.Location + previousEntry.Input.Length != entry.Location) continue;
				//merge entries next to each other
				var mergedEntry = new Entry
				{
					Input = $"{previousEntry.Input}{entry.Input}",
					Output = $"{previousEntry.Output} {entry.Output}",
					RoleString = "m",
					Regex = previousEntry.Regex || entry.Regex
				};
				entriesToAdd.Add(mergedEntry);
				entriesToRemove.Add(previousEntry);
				entriesToRemove.Add(entry);
			}
			foreach (var entry in entriesToRemove) entries.Remove(entry);
			foreach (var entry in entriesToAdd) entries.Add(entry);
		}

        /// <summary>
        /// Removes entries from the list, when there are other entries that contain their input.
        /// </summary>
        /// <param name="entries">List of entries, will be modified</param>
        /// <example>
        /// 2 entries: おかあーさん,かあーさん
        /// Input string: おかあーさんはどこですか？
        /// We don't need to keep the second entry because it's included in the first.
        /// In a different example:
        /// Input string: おかあーさんとか、かあーさんとか
        /// We don't remove the second entry because it stands on its own.
        /// </example>
        private static void RemoveSubEntries(ICollection<Entry> entries)
		{
			foreach (var entry in entries.ToList())
			{
				var superEntry = entries.FirstOrDefault(e => e != entry && e.Input.Contains(entry.Input));
				if (superEntry == null) continue;
				//we use the location in the string to be translated, to ensure we don't remove a proxy for an input that stands on its own
				//example:　おかあーさんとか、かあーさんとか
				//given that we have proxies for おかあーさん and かあーさん, we want to keep them both so that the latter is also translated.
				var superEntryEndLocation = superEntry.Location + superEntry.Input.Length;
				if (entry.Location > superEntry.Location && entry.Location < superEntryEndLocation) entries.Remove(entry);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void LogReplace(StringBuilder sb, string input, string output, TranslationResults result, Entry entry, bool caseInsensitive)
		{
			if (_logVerbose || (result != null && result.SaveData && entry != null))
			{
				var sbOriginal = sb.ToString();
                if (caseInsensitive)
                {
                    sb.Clear();
                    sb.Append(Regex.Replace(sbOriginal, Regex.Escape(input), output, RegexOptions.IgnoreCase));
                }
				else sb.Replace(input, output);
				var sbReplaced = sb.ToString();
				if (sbOriginal == sbReplaced) return;
				if (_logVerbose) StaticHelpers.Logger.Verbose($"Replace happened - Id {entry?.Id.ToString() ?? "N/A"}: '{input}' > '{output}'");
				if (result.SaveData) result.AddEntryUsed(entry);
			}
			else sb.Replace(input, output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void LogReplace(StringBuilder sb, Entry entry, TranslationResults result, bool caseInsensitive)
		{
			LogReplace(sb, entry.Input, entry.Output, result, entry, caseInsensitive);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void LogReplaceRegex(StringBuilder sb, string regexInput, string output, TranslationResults result, Entry entry)
		{
			string replaced;
			var rgx = GetRegex(regexInput);
			if (_logVerbose || (result != null && result.SaveData && entry != null))
			{
				var sbOriginal = sb.ToString();
				replaced = rgx.Replace(sbOriginal, output);
				if (sbOriginal != replaced)
				{
					if (_logVerbose) StaticHelpers.Logger.Verbose($"Replace happened - Id {entry?.Id.ToString() ?? "N/A"} '{regexInput}' > '{output}'");
					if (result.SaveData) result.AddEntryUsed(entry);
				}
			}
			else replaced = rgx.Replace(sb.ToString(), output);
			sb.Clear();
			sb.Append(replaced);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void LogReplaceRegex(StringBuilder sb, Entry entry, TranslationResults result)
		{
			LogReplaceRegex(sb, entry.Input, entry.Output, result, entry);
		}

		/// <summary>
		/// Character is between points \u3040 and \u309f
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsHiragana(char character) => character >= 0x3040 && character <= 0x309f;

		/// <summary>
		/// Character is between points \u30a0 and \u30ff
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsKatakana(char character) => character >= 0x30a0 && character <= 0x30ff;

		private bool GetFromCache(string cacheSource, StringBuilder text, string input, TranslationResults result)
		{
			//todo keep cache collection grouped by source and change primary key to input + source
			var item = cacheSource == null ? _data.Translations[input] : _data.Translations.FirstOrDefault(i => i.Source == cacheSource && i.Key == input);
			if (item == null) return false;
			LogVerbose($"{nameof(Translator)} - Getting string from cache ({cacheSource ?? "null"}), input: {input}");
			GotFromCacheCount++;
			item.Update();
			_data.Translations.UpsertLater(item);
			text.Append(item.Output);
			if (result?.SaveData ?? false) result.AddTranslationUsed(item);
			return true;
		}

		private bool TryGetWithoutApi(string cacheSource, StringBuilder text, bool isBlocked, TranslationResults result, out string input)
		{
			input = text.ToString();
			if (_settings.UntouchedStrings.Contains(input)) return true;
			text.Clear();
			if (GetFromCache(cacheSource, text, input, result)) return true;
			if (TranslateSingleKana(text, input)) return true;
			if (!isBlocked) return false;
			text.Append("Failed: Translation is blocked.");
			return true;
		}

		private bool TranslateSingleKana(StringBuilder text, string input)
		{
			if (input.Length != 1) return false;
			var character = input[0];
			if (!IsHiragana(character) && !IsKatakana(character)) return false;
			//if character is 'tsu' on its own, we remove it.
			var output = character == 'っ' || character == 'ッ' ? string.Empty : GetRomaji(input);
			text.Clear();
			SetTranslationAndSaveToCache(text, output, input, "Single Kana", null);
			return true;
		}

		private void SetTranslationAndSaveToCache(StringBuilder text, string translated, string input, string sourceName, TranslationResults result)
		{
			GotFromApiCount++;
			text.Append(translated);
			var translation = new CachedTranslation(input, translated, sourceName, _lastGame.GameId, _lastGame.IsUserGame);
			_data.Translations.UpsertLater(translation);
            if (result?.SaveData ?? false) result.AddTranslationUsed(translation);
        }

		/// <summary>
		/// Tries to get a translation from cache, if not, tries to use selected translator and saves result to cache if successful.
		/// </summary>
		private void MachineTranslate(StringBuilder text, TranslationResults result)
		{
			if (TryGetWithoutApi(_useAnyCached ? null : SelectedTranslator.SourceName, text, false, result, out var input)) return;
			if (!string.IsNullOrWhiteSpace(SelectedTranslator.Error))
			{
				text.Append(SelectedTranslator.Error);
				return;
			}
			var success = SelectedTranslator.Translate(input, out var translated);
			if (success) SetTranslationAndSaveToCache(text, translated, input, SelectedTranslator.SourceName, result);
			else text.Append(translated);
		}

		private void LogVerbose(string text)
		{
			if (!_logVerbose) return;
			Debug.WriteLine(text);
		}

		public static void ExitProcedures(Func<int> saveData)
		{
			Debug.WriteLine($"[{nameof(Translator)}] Got from cache {GotFromCacheCount}");
			Debug.WriteLine($"[{nameof(Translator)}] Got from API {GotFromApiCount}");
			saveData();
		}
	}
}
