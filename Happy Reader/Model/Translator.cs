using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using HRGoogleTranslate;
using System.Diagnostics;
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
		private static readonly Dictionary<string, Regex> RegexDict = new Dictionary<string, Regex>();

		private readonly HappyReaderDatabase _data;
		private User _lastUser;
		private ListedVN _lastGame;
		private Entry[] _entries;
		public bool RefreshEntries = true;

		public Translator(HappyReaderDatabase data) => _data = data;

		public void SetCache(bool noApiTranslation)
		{
			var cachedTranslations = _data.CachedTranslations.Local;
			GoogleTranslate.Initialize(
				cachedTranslations.ToDictionary(x => x.Input),
				cachedTranslations,
				Kakasi.JapaneseToRomaji,
				StaticMethods.TranslatorSettings.GoogleCredentialPath,
				StaticMethods.TranslatorSettings.FreeUserAgent,
				StaticMethods.TranslatorSettings.UntouchedStrings,
				noApiTranslation);
		}

		public Translation Translate(User user, ListedVN game, string input)
		{
			input = input.Replace("\r", "");
			if (string.IsNullOrWhiteSpace(input)) return null;
			if (input.Length > StaticMethods.TranslatorSettings.MaxClipboardSize) return null; //todo report error
			if (LatinOnlyRegex.IsMatch(input)) return null;
			input = input.Replace("\r\n", "");
			input = CheckRepeatedString(input);
			if (string.IsNullOrWhiteSpace(input)) return null;
			lock (TranslateLock)
			{
				if (user != _lastUser || game != _lastGame || RefreshEntries) SetEntries(user, game);
				var item = new Translation(input);
				SplitInputIntoParts(item);
				item.TranslateParts();
				return item;
			}
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
						item.Parts.Add((currentPart, !currentPart.All(c => Separators.Contains(c))));
					}
					else
					{
						if (currentPart.Length > 0) item.Parts.Add((currentPart, !currentPart.All(c => Separators.Contains(c))));
						item.Parts.Add((@char.ToString(), false));
					}
					currentPart = "";
					index++;
					continue;
				}
				currentPart += @char;
				index++;
			}
			if (currentPart.Length > 0) item.Parts.Add((currentPart, !currentPart.All(c => Separators.Contains(c))));
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
			Debug.WriteLine($"General entries: {generalEntries.Length}. Specific entries: {specificEntries.Length}");
		}

		/// <summary>
		/// Replace entries of type Game.
		/// </summary>
		public void TranslateStageOne(StringBuilder sb, string[] result)
		{
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Game))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry);
				else LogReplace(sb, entry);
			}
			StaticHelpers.Logger.Verbose($"Stage 1: {sb}");
			//Stage One is also used for Translation.Original which does not require setting result to array.
			if (result != null) result[1] = sb.ToString();
		}

		/// <summary>
		/// Replace entries of type Input.
		/// </summary>
		private void TranslateStageTwo(StringBuilder sb, string[] result)
		{
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Input))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry);
				else LogReplace(sb, entry);
			}
			StaticHelpers.Logger.Verbose($"Stage 2: {sb}");
			result[2] = sb.ToString();
		}

		/// <summary>
		/// Replace entries of type Yomi.
		/// </summary>
		private void TranslateStageThree(StringBuilder sb, string[] result)
		{
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Yomi)) LogReplace(sb, entry);
			StaticHelpers.Logger.Verbose($"Stage 3: {sb}");
			result[3] = sb.ToString();
		}

		public void ReplacePreRomaji(StringBuilder sb)
		{
			foreach (var entry in _entries.Where(x => x.Type == EntryType.Name || x.Type == EntryType.Yomi || x.Type == EntryType.PreRomaji))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry);
				else LogReplace(sb, entry);
			}
		}

		public void ReplacePostRomaji(StringBuilder sb)
		{
			foreach (var entry in _entries.Where(x => x.Type == EntryType.PostRomaji))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry);
				else LogReplace(sb, entry);
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
		private IEnumerable<Entry> TranslateStageFour(StringBuilder sb, HappyReaderDatabase data, string[] result)
		{
			var usefulEntriesWithProxies = GetRelevantEntriesWithProxies(sb, data, out Dictionary<string, ProxiesWithCount> proxies);
			if (usefulEntriesWithProxies.Count == 0) return usefulEntriesWithProxies;
			foreach (var entry in usefulEntriesWithProxies)
			{
				var proxyAssigned = AssignProxy(proxies, entry);
				if (proxyAssigned) LogReplace(sb, entry.Input, entry.AssignedProxy.FullRoleString, entry.Id);
			}
			usefulEntriesWithProxies = usefulEntriesWithProxies.Where(e => e.AssignedProxy != null).ToList();
			StaticHelpers.Logger.Verbose($"Stage 4.0: {sb}");
			//perform replaces involving proxies
			var entriesOnProxies = _entries.Where(i => i.Type == EntryType.ProxyMod).ToArray();
			TranslateStage4P1(sb, usefulEntriesWithProxies, entriesOnProxies);
			foreach (var entry in usefulEntriesWithProxies)
			{
				LogReplace(sb, entry.AssignedProxy.FullRoleString, entry.AssignedProxy.Entry.Input, entry.Id);
			}
			StaticHelpers.Logger.Verbose($"Stage 4.2: {sb}");
			result[4] = sb.ToString();
			return usefulEntriesWithProxies;
		}

		private List<Entry> GetRelevantEntriesWithProxies(StringBuilder sb, HappyReaderDatabase data, out Dictionary<string, ProxiesWithCount> proxies)
		{
			var roles = _entries.Select(z => z.RoleString).Distinct().ToArray();
			proxies = BuildProxiesList(data, roles);
			var entriesWithProxiesArray = _entries.Where(i => i.Type == EntryType.Name || i.Type == EntryType.Translation)
				.OrderByDescending(x => x.Input.Length).ToArray();
			var usefulEntriesWithProxies = entriesWithProxiesArray.OrderByDescending(x => x.Input.Length).ToList();
			//remove unused entries
			foreach (var entry in entriesWithProxiesArray)
			{
				int? location = null;
				if (entry.Regex)
				{
					var match = GetRegex(entry.Input).Match(sb.ToString());
					if (match.Success) location = match.Index;
				}
				else
				{
					var indexOfEntry = sb.ToString().IndexOf(entry.Input, StringComparison.Ordinal);
					if (indexOfEntry >= 0) location = indexOfEntry;
				}
				if(location == null) usefulEntriesWithProxies.Remove(entry);
			}
			if (usefulEntriesWithProxies.Count == 0) return usefulEntriesWithProxies;
			usefulEntriesWithProxies = usefulEntriesWithProxies.OrderBy(x => x.Location).ToList();
			RemoveSubEntries(usefulEntriesWithProxies);
			//merge entries that are together
			var entriesToRemove = new List<Entry>();
			var entriesToAdd = new List<Entry>();
			for (var index = 1; index < usefulEntriesWithProxies.Count; index++)
			{
				var previousEntry = usefulEntriesWithProxies[index - 1];
				var entry = usefulEntriesWithProxies[index];
				if (previousEntry.Location + previousEntry.Input.Length != entry.Location) continue;
				//merge entries next to each other
				var mergedEntry = new Entry
				{
					Input = $"{previousEntry.Input}{entry.Input}",
					Output = $"{previousEntry.Output} {entry.Output}",
					RoleString = "m"
				};
				entriesToAdd.Add(mergedEntry);
				entriesToRemove.Add(previousEntry);
				entriesToRemove.Add(entry);
			}
			foreach (var entry in entriesToRemove) usefulEntriesWithProxies.Remove(entry);
			foreach (var entry in entriesToAdd) usefulEntriesWithProxies.Add(entry);
			return usefulEntriesWithProxies;
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
		private static void RemoveSubEntries(List<Entry> entriesWithProxies)
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

		private Dictionary<string, ProxiesWithCount> BuildProxiesList(HappyReaderDatabase data, string[] roles)
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

		private bool AssignProxy(Dictionary<string, ProxiesWithCount> proxies, Entry entry)
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
				var input = Regex.Replace(entry.Input, @"\[\[(.+?)]]", @"\[\[$1#(\d+)]]");
				var matches = Regex.Matches(sb.ToString(), input).Cast<Match>().Select(x => Int32.Parse(x.Groups[1].Value)).Distinct();
				foreach (int match in matches)
				{
					entriesWithProxies.Single(x => x.AssignedProxy.Id == match).AssignedProxy.ProxyMods.Add(entry);
				}
				var output = new Regex(@"^.*?\[\[(.+)]].*?$").Replace(entry.Output, @"[[$1#$$1]]");
				LogReplaceRegex(sb, input, output, entry.Id);
			}
			StaticHelpers.Logger.Verbose($"Stage 4.1: {sb}");
		}

		public string[] TranslatePart(string input)
		{
			var result = new string[8];
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
			if (StaticMethods.TranslatorSettings.GoogleUseCredential) GoogleTranslate.Translate(sb);
			else GoogleTranslate.TranslateFree(sb);
			StaticHelpers.Logger.Verbose($"Stage 5: {sb}");
			result[5] = sb.ToString();
			TranslateStageSix(sb, usefulEntriesWithProxies, result);
			TranslateStageSeven(sb, result);
			return result;
		}

		/// <summary>
		/// Replace Name and Translation proxies to entry outputs.
		/// </summary>
		private static void TranslateStageSix(StringBuilder sb, IEnumerable<Entry> usefulEntriesWithProxies, string[] result)
		{
			foreach (var entry in usefulEntriesWithProxies)
			{
				LogReplace(sb, entry.AssignedProxy.Entry.Output, entry.AssignedProxy.FullRoleString, entry.Id);
				foreach (var proxyMod in entry.AssignedProxy.ProxyMods.AsEnumerable().Reverse())
				{
					var pmO = proxyMod.Output.Replace($"[[{proxyMod.RoleString ?? "m"}]]", entry.AssignedProxy.FullRoleString);
					LogReplace(sb, entry.AssignedProxy.FullRoleString, pmO, proxyMod.Id);
				}
				LogReplace(sb, entry.AssignedProxy.FullRoleString, entry.Output, entry.Id);
			}
			StaticHelpers.Logger.Verbose($"Stage 6: {sb}");
			result[6] = sb.ToString();
		}

		/// <summary>
		/// Replace entries of type Output.
		/// </summary>
		private void TranslateStageSeven(StringBuilder sb, string[] result)
		{
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Output && !i.Regex)) LogReplace(sb, entry.Input, entry.Output, entry.Id);
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Output && i.Regex)) LogReplaceRegex(sb, entry.Input, entry.Output, entry.Id);
			StaticHelpers.Logger.Verbose($"Stage 7: {sb}");
			result[7] = sb.ToString();
		}

		private static string CheckRepeatedString(string text)
		{
			if (text.Length < 5) return text;
			var evenCharacters = string.Join("", text.Where((x, i) => i % 2 == 0));
			var oddCharacters = string.Join("", text.Where((x, i) => i % 2 == 1));
			if (evenCharacters == oddCharacters) return evenCharacters;
			//check if repeated after name
			var firstBracket = text.IndexOfAny(new[] { '「', '『' });
			if (firstBracket <= 0) return ReduceRepeatedString(text);
			var name = text.Substring(0, firstBracket);
			var checkText = text.Substring(firstBracket);
			return name + ReduceRepeatedString(checkText);
		}

		private static string ReduceRepeatedString(string text)
		{
			if (text.Length % 2 != 0) return text;
			var halfLength = text.Length / 2;
			var p1 = text.Substring(0, halfLength);
			var p2 = text.Substring(halfLength);
			return p1 == p2 ? p1 : text;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// ReSharper disable once UnusedParameter.Local
		private static void LogReplace(StringBuilder sb, string input, string output, long id)
		{
#if LOGVERBOSE
            var sbOriginal = sb.ToString();
            sb.Replace(input, output);
            var sbReplaced = sb.ToString();
            if (sbOriginal != sbReplaced)
            {
                Debug.WriteLine($"Replace happened - Id {id}: '{input}' > '{output}'");
            }
#else
			sb.Replace(input, output);
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// ReSharper disable once UnusedParameter.Local
		private static void LogReplace(StringBuilder sb, Entry entry)
		{
			LogReplace(sb, entry.Input, entry.Output, entry.Id);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// ReSharper disable once UnusedParameter.Local
		private static void LogReplaceRegex(StringBuilder sb, string regexInput, string output, long id)
		{
			var rgx = GetRegex(regexInput);
#if LOGVERBOSE
      var sbOriginal = sb.ToString();
      var sbReplaced = rgx.Replace(sbOriginal, output);
      if (sbOriginal != sbReplaced) Debug.WriteLine($"Replace happened - Id {id}: '{regexInput}' > '{output}'");
#else
			var sbReplaced = rgx.Replace(sb.ToString(), output);
#endif
			sb.Clear();
			sb.Append(sbReplaced);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// ReSharper disable once UnusedParameter.Local
		private static void LogReplaceRegex(StringBuilder sb, Entry entry)
		{
			LogReplaceRegex(sb, entry.Input, entry.Output, entry.Id);
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
