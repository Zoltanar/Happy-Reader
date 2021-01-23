using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using HRGoogleTranslate;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Happy_Reader
{
	public class Translator
	{
		//TODO add regex to all stages

		private static readonly object TranslateLock = new object();
		private static readonly char[] Separators = "『「」』…".ToCharArray();
		private static readonly char[] InclusiveSeparators = "。？".ToCharArray();
		private static readonly char[] AllSeparators = Separators.Concat(InclusiveSeparators).ToArray();
		private static readonly Regex LatinOnlyRegex = new Regex(@"^[a-zA-Z0-9:/\\\\r\\n .!?,;()_$^""]+$");
		private static readonly Regex Stage4P1InputRegex = new Regex(@"\[\[(.+?)]]");
		private static readonly Regex Stage4P1OutputRegex = new Regex(@"^.*?\[\[(.+)]].*?$");
		private static readonly Dictionary<string, Regex> RegexDict = new Dictionary<string, Regex>();

		private readonly HappyReaderDatabase _data;
		private User _lastUser;
		private ListedVN _lastGame;
		private Entry[] _entries;
		private bool _logVerbose;
		public bool RefreshEntries = true;

		public Translator(HappyReaderDatabase data) => _data = data;

		public void SetCache(bool noApiTranslation, bool logVerbose)
		{
			_logVerbose = logVerbose;
			var cachedTranslations = _data.CachedTranslations.Local;
			GoogleTranslate.Initialize(
				cachedTranslations.ToDictionary(x => x.Input),
				cachedTranslations,
				Kakasi.JapaneseToRomaji,
				StaticMethods.TranslatorSettings.GoogleCredentialPath,
				StaticMethods.TranslatorSettings.FreeUserAgent,
				StaticMethods.TranslatorSettings.UntouchedStrings,
				noApiTranslation,
				logVerbose);
		}

		public Translation Translate(User user, ListedVN game, string input, bool saveEntriesUsed, bool removeRepetition)
		{
			if (removeRepetition)
			{
				int loopCount = 0;
				while (true)
				{
					if(loopCount > 1000) throw new ArgumentException($"Loop executed {loopCount} times, likely to be stuck.");
					var before = input;
					input = ReduceRepeatedString(before);
					if (input == before) break;
					loopCount++;
				}
			}
			input = input.Replace("\r", "");
			if (string.IsNullOrWhiteSpace(input)) return new Translation(input, false);
			if (input.Length > StaticMethods.TranslatorSettings.MaxClipboardSize) return null; //todo report error
			input = input.Replace("\r\n", "");
			if (string.IsNullOrWhiteSpace(input)) return new Translation(input, false);
			lock (TranslateLock)
			{
				if (user != _lastUser || game != _lastGame || RefreshEntries) SetEntries(user, game);
				if (LatinOnlyRegex.IsMatch(input)) return new Translation(input, false);
				var item = new Translation(input, true);
				SplitInputIntoParts(item);
				item.TranslateParts(saveEntriesUsed);
				return item;
			}
		}

		/// <summary>
		/// If start of input is repeated, it is removed.
		/// </summary>
		/// <param name="input">String to be reduced.</param>
		/// <returns>String without repeated segment at the start.</returns>
		private string ReduceRepeatedString(string input)
		{
			if (input.Length == 1) return input;
			var firstChar = input[0];
			var indexOfSecondBracket = input.IndexOf(firstChar, 1);
			if (indexOfSecondBracket == -1) return input;
			int skip = 0;
			while (input[skip] == input[indexOfSecondBracket + skip] && skip < indexOfSecondBracket) skip++;
			return skip != indexOfSecondBracket ? input : input.Substring(skip);
		}

		private static void SplitInputIntoParts(Translation item)
		{
			var input = item.Original;
			int index = 0;
			string currentPart = "";
			while (index < input.Length)
			{
				var @char = input[index];
				if (AllSeparators.Contains(@char))
				{
					if (InclusiveSeparators.Contains(@char))
					{
						currentPart += @char;
						item.Parts.Add((currentPart, !currentPart.All(c => AllSeparators.Contains(c))));
					}
					else
					{
						if (currentPart.Length > 0) item.Parts.Add((currentPart, !currentPart.All(c => AllSeparators.Contains(c))));
						item.Parts.Add((@char.ToString(), false));
					}
					currentPart = "";
					index++;
					continue;
				}
				currentPart += @char;
				index++;
			}
			if (currentPart.Length > 0) item.Parts.Add((currentPart, !currentPart.All(c => AllSeparators.Contains(c))));
		}

		private void SetEntries([NotNull] User user, ListedVN game)
		{
			RefreshEntries = false;
			_lastUser = user;
			_lastGame = game;
			int[] gamesInSeries = null;
			if (game != null)
			{
				gamesInSeries = string.IsNullOrWhiteSpace(game.Series)
					? new[] { game.VNID }
					: StaticHelpers.LocalDatabase.VisualNovels.Where(g => g.Series == game.Series).Select(gg => gg.VNID).ToArray();
			}
			Entry[] generalEntries = _data.Entries.Where(e => (e.Private && e.UserId == user.Id || !e.Private) && !e.SeriesSpecific).ToArray();
			Entry[] specificEntries = { };
			if (gamesInSeries != null)
			{
				specificEntries = _data.Entries.Where(e => (e.Private && e.UserId == user.Id || !e.Private) && e.SeriesSpecific && gamesInSeries.Contains(e.GameId.Value)).ToArray();
			}
			_entries = generalEntries.Concat(specificEntries).OrderBy(i => i.Id).ToArray();
			StaticHelpers.Logger.ToDebug($"[Translator] General entries: {generalEntries.Length}. Specific entries: {specificEntries.Length}");
		}

		/// <summary>
		/// Replace entries of type Game.
		/// </summary>
		public void TranslateStageOne(StringBuilder sb, TranslationResults result)
		{
			result?.SetStage(1);
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Game))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, result);
				else LogReplace(sb, entry, result);
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
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Input))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, result);
				else LogReplace(sb, entry, result);
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
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Yomi)) LogReplace(sb, entry, result);
			StaticHelpers.Logger.Verbose($"Stage 3: {sb}");
			result[3] = sb.ToString();
		}

		public void ReplacePreRomaji(StringBuilder sb)
		{
			var entries = _entries.Where(x => x.Type == EntryType.Name || x.Type == EntryType.Yomi || x.Type == EntryType.PreRomaji).ToArray();
			var usefulEntries = RemoveUnusedEntriesAndSetLocation(sb, entries);
			MergeNeighbouringEntries(usefulEntries);
			foreach (var entry in usefulEntries)
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, null);
				else LogReplace(sb, entry, null);
			}
		}

		public void ReplacePostRomaji(StringBuilder sb)
		{
			foreach (var entry in _entries.Where(x => x.Type == EntryType.PostRomaji))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry, null);
				else LogReplace(sb, entry, null);
			}
		}

		private static Regex GetRegex(string input)
		{
			if (!RegexDict.TryGetValue(input, out var regex))
			{
				regex = RegexDict[input] = new Regex(input);
			}
			return regex;
		}

		/// <summary>
		/// Replace entries of type Name and translation to proxies.
		/// </summary>
		private IEnumerable<Entry> TranslateStageFour(StringBuilder sb, HappyReaderDatabase data, TranslationResults result)
		{
			result.SetStage(4);
			var usefulEntriesWithProxies = GetRelevantEntriesWithProxies(sb, data, out Dictionary<string, ProxiesWithCount> proxies);
			if (usefulEntriesWithProxies.Count != 0)
			{
				foreach (var entry in usefulEntriesWithProxies)
				{
					var proxyAssigned = AssignProxy(proxies, entry);
					if (proxyAssigned) LogReplace(sb, entry.Input, entry.AssignedProxy.FullRoleString, result, entry);
				}
				usefulEntriesWithProxies = usefulEntriesWithProxies.Where(e => e.AssignedProxy != null).ToList();
				StaticHelpers.Logger.Verbose($"Stage 4.0: {sb}");
				//perform replaces involving proxies
				var entriesOnProxies = _entries.Where(i => i.Type == EntryType.ProxyMod).ToArray();
				TranslateStage4P1(sb, usefulEntriesWithProxies, entriesOnProxies);
				foreach (var entry in usefulEntriesWithProxies)
				{
					foreach (var proxyMod in entry.AssignedProxy.ProxyMods) result.AddEntryUsed(proxyMod);
					LogReplace(sb, entry.AssignedProxy.FullRoleString, entry.AssignedProxy.Entry.Input, result, entry);
				}
				StaticHelpers.Logger.Verbose($"Stage 4.2: {sb}");
			}
			result[4] = sb.ToString();
			return usefulEntriesWithProxies;
		}

		private List<Entry> GetRelevantEntriesWithProxies(StringBuilder sb, HappyReaderDatabase data, out Dictionary<string, ProxiesWithCount> proxies)
		{
			var roles = _entries.Select(z => z.RoleString).Distinct().ToArray();
			proxies = BuildProxiesList(data, roles);
			var entriesWithProxiesArray = _entries.Where(i => i.Type == EntryType.Name || i.Type == EntryType.Translation)
				.OrderByDescending(x => x.Input.Length).ToArray();
			var usefulEntriesWithProxies = RemoveUnusedEntriesAndSetLocation(sb, entriesWithProxiesArray);
			if (usefulEntriesWithProxies.Count == 0) return usefulEntriesWithProxies;
			RemoveSubEntries(usefulEntriesWithProxies);
			MergeNeighbouringEntries(usefulEntriesWithProxies);
			return usefulEntriesWithProxies;
		}

		private static List<Entry> RemoveUnusedEntriesAndSetLocation(StringBuilder sb, IList<Entry> entries)
		{
			var text = sb.ToString();
			List<Entry> relevantEntries = entries.OrderByDescending(x => x.Input.Length).ToList();
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
				if (!location.HasValue) relevantEntries.Remove(entry);
				else entry.Location = location.Value;
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
		/// <param name="entriesWithProxies">List of entries, will be modified</param>
		/// <example>
		/// 2 entries: おかあーさん,かあーさん
		/// Input string: おかあーさんはどこですか？
		/// We don't need to keep the second entry because it's included in the first.
		/// In a different example:
		/// Input string: おかあーさんとか、かあーさんとか
		/// We don't remove the second entry because it stands on its own.
		/// </example>
		private static void RemoveSubEntries(ICollection<Entry> entriesWithProxies)
		{
			foreach (var entry in entriesWithProxies.ToList())
			{
				var superEntry = entriesWithProxies.FirstOrDefault(e => e != entry && e.Input.Contains(entry.Input));
				if (superEntry == null) continue;
				//we use the location in the string to be translated, to ensure we don't remove a proxy for an input that stands on its own
				//example:　おかあーさんとか、かあーさんとか
				//given that we have proxies for おかあーさん and かあーさん, we want to keep them both so that the latter is also translated.
				var superEntryEndLocation = superEntry.Location + superEntry.Input.Length;
				if (entry.Location > superEntry.Location && entry.Location < superEntryEndLocation) entriesWithProxies.Remove(entry);
			}
		}

		private Dictionary<string, ProxiesWithCount> BuildProxiesList(HappyReaderDatabase data, IEnumerable<string> roles)
		{
			var proxies = new Dictionary<string, ProxiesWithCount>();
			foreach (var role in roles)
			{
				if (role == null) continue;
				Entry[] roleProxies = data.Entries.Where(i => i.Type == EntryType.Proxy && i.RoleString == role).ToArray();
				if (proxies.ContainsKey(role))
				{
					foreach (var roleProxy in roleProxies.Select(
						roleProxy => new RoleProxy { Role = role, Entry = roleProxy }))
					{
						proxies[role].Proxies.Enqueue(roleProxy);
					}
				}
				else proxies.Add(role, new ProxiesWithCount(roleProxies.Select(roleProxy => new RoleProxy { Role = role, Entry = roleProxy })));
			}
			return proxies;
		}

		private bool AssignProxy(IReadOnlyDictionary<string, ProxiesWithCount> proxies, Entry entry)
		{
			var proxy = proxies[entry.RoleString].Proxies.Count == 0 ? null : proxies[entry.RoleString].Proxies.Dequeue();
			proxies[entry.RoleString].Count++;
			if (proxy == null)
			{
				StaticHelpers.Logger.ToFile("No proxy available, won't proxy-translate.");
				throw new Exception("Error - no proxy available.");
			}
			proxy.FullRoleString = $"[[{entry.RoleString}#{proxies[entry.RoleString].Count}]]";
			proxy.Id = proxies[entry.RoleString].Count;
			entry.AssignedProxy = proxy;
			return true;
		}

		private void TranslateStage4P1(StringBuilder sb, IReadOnlyCollection<Entry> entriesWithProxies, IEnumerable<Entry> entriesOnProxies)
		{
			foreach (var entry in entriesOnProxies)
			{
				var input = Stage4P1InputRegex.Replace(entry.Input, @"\[\[$1#(\d+)]]");
				var matches = Regex.Matches(sb.ToString(), input).Cast<Match>().Select(x => int.Parse(x.Groups[1].Value)).Distinct();
				foreach (int match in matches)
				{
					entriesWithProxies.Single(x => x.AssignedProxy.Id == match && x.AssignedProxy.Role == entry.RoleString).AssignedProxy.ProxyMods.Add(entry);
				}
				var output = Stage4P1OutputRegex.Replace(entry.Output, @"[[$1#$$1]]");
				LogReplaceRegex(sb, input, output, null, entry);
			}
			StaticHelpers.Logger.Verbose($"Stage 4.1: {sb}");
		}

		public TranslationResults TranslatePart(string input, bool saveEntriesUsed)
		{
			var result = new TranslationResults(saveEntriesUsed);
			var sb = new StringBuilder(input);
			if (GoogleTranslate.TranslateSingleKana(sb, input))
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
			TranslateStageOne(sb, result);
			TranslateStageTwo(sb, result);
			TranslateStageThree(sb, result);
			IEnumerable<Entry> usefulEntriesWithProxies;
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
			TranslateStageFive(sb, result);
			TranslateStageSix(sb, usefulEntriesWithProxies, result);
			TranslateStageSeven(sb, result);
			return result;
		}

		private static void TranslateStageFive(StringBuilder sb, TranslationResults result)
		{
			result.SetStage(5);
			if (StaticMethods.TranslatorSettings.GoogleUseCredential) GoogleTranslate.Translate(sb);
			else GoogleTranslate.TranslateFree(sb);
			StaticHelpers.Logger.Verbose($"Stage 5: {sb}");
			result[5] = sb.ToString();
		}

		/// <summary>
		/// Replace Name and Translation proxies to entry outputs.
		/// </summary>
		private void TranslateStageSix(StringBuilder sb, IEnumerable<Entry> usefulEntriesWithProxies, TranslationResults result)
		{
			result.SetStage(6);
			foreach (var entry in usefulEntriesWithProxies)
			{
				LogReplace(sb, entry.AssignedProxy.Entry.Output, entry.AssignedProxy.FullRoleString, result, entry);
				foreach (var proxyMod in entry.AssignedProxy.ProxyMods.AsEnumerable().Reverse())
				{
					var pmO = proxyMod.Output.Replace($"[[{proxyMod.RoleString ?? "m"}]]", entry.AssignedProxy.FullRoleString);
					LogReplace(sb, entry.AssignedProxy.FullRoleString, pmO, result, entry);
				}
				LogReplace(sb, entry.AssignedProxy.FullRoleString, entry.Output, result, entry);
			}
			StaticHelpers.Logger.Verbose($"Stage 6: {sb}");
			result[6] = sb.ToString();
		}

		/// <summary>
		/// Replace entries of type Output.
		/// </summary>
		private void TranslateStageSeven(StringBuilder sb, TranslationResults result)
		{
			result.SetStage(7);
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Output && !i.Regex)) LogReplace(sb, entry.Input, entry.Output, result, entry);
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Output && i.Regex)) LogReplaceRegex(sb, entry.Input, entry.Output, result, entry);
			StaticHelpers.Logger.Verbose($"Stage 7: {sb}");
			result[7] = sb.ToString();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void LogReplace(StringBuilder sb, string input, string output, TranslationResults result, Entry entry)
		{
			if (_logVerbose || (result != null && result.SaveEntries && entry != null))
			{
				var sbOriginal = sb.ToString();
				sb.Replace(input, output);
				var sbReplaced = sb.ToString();
				if (sbOriginal == sbReplaced) return;
				if (_logVerbose) StaticHelpers.Logger.Verbose($"Replace happened - Id {entry?.Id.ToString() ?? "N/A"}: '{input}' > '{output}'");
				if (result.SaveEntries) result.AddEntryUsed(entry);
			}
			else sb.Replace(input, output);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void LogReplace(StringBuilder sb, Entry entry, TranslationResults result)
		{
			LogReplace(sb, entry.Input, entry.Output, result, entry);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void LogReplaceRegex(StringBuilder sb, string regexInput, string output, TranslationResults result, Entry entry)
		{
			string replaced;
			var rgx = GetRegex(regexInput);
			if (_logVerbose || (result != null && result.SaveEntries && entry != null))
			{
				var sbOriginal = sb.ToString();
				replaced = rgx.Replace(sbOriginal, output);
				if (sbOriginal != replaced)
				{
					if (_logVerbose) StaticHelpers.Logger.Verbose($"Replace happened - Id {entry?.Id.ToString() ?? "N/A"} '{regexInput}' > '{output}'");
					if (result.SaveEntries) result.AddEntryUsed(entry);
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

		private class ProxiesWithCount
		{
			public Queue<RoleProxy> Proxies { get; }
			public int Count { get; set; }

			public ProxiesWithCount(IEnumerable<RoleProxy> proxies)
			{
				Proxies = new Queue<RoleProxy>(proxies);
				Count = 0;
			}
		}
	}
}
