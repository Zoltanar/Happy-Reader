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

		private readonly HappyReaderDatabase _data;
		private User _lastUser;
		private ListedVN _lastGame;
		private Entry[] _entries;
		public bool RefreshEntries;

		public Translator(HappyReaderDatabase data) => _data = data;

		public void SetCache() => GoogleTranslate.Initialize(_data.CachedTranslations.Local, Kakasi.JapaneseToRomaji);

		public Translation Translate(User user, ListedVN game, string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return null;
			if (input.Length > StaticHelpers.GSettings.MaxClipboardSize) return null; //todo report error
			if (LatinOnlyRegex.IsMatch(input)) return null;
			input = input.Replace("\r\n", "");
			input = CheckRepeatedString(input);
			if (string.IsNullOrWhiteSpace(input)) return null;
			lock (TranslateLock)
			{
				if (user != _lastUser || game != _lastGame || RefreshEntries) SetEntries(user, game);
				var item = new Translation(input);
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
				item.TranslateParts();
				return item;
			}
		}

		private void SetEntries([NotNull]User user, ListedVN game)
		{
			RefreshEntries = false;
			_lastUser = user;
			_lastGame = game;
			int[] gamesInSeries = null;
			if (game != null)
			{
				gamesInSeries = game.Series == null
					? new[] { game.VNID }
					: StaticHelpers.LocalDatabase.LocalVisualNovels.Where(g => g.Series == game.Series).Select(gg => gg.VNID).ToArray();
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
		public void TranslateStageOne(StringBuilder sb)
		{
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Game))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry.Input, entry.Output, entry.Id);
				else LogReplace(sb, entry.Input, entry.Output, entry.Id);
			}
			StaticHelpers.LogVerbose($"Stage 1: {sb}");
		}

		/// <summary>
		/// Replace entries of type Input.
		/// </summary>
		private void TranslateStageTwo(StringBuilder sb)
		{

			foreach (var entry in _entries.Where(i => i.Type == EntryType.Input))
			{
				if (entry.Regex) LogReplaceRegex(sb, entry.Input, entry.Output, entry.Id);
				else LogReplace(sb, entry.Input, entry.Output, entry.Id);
			}
            StaticHelpers.LogVerbose($"Stage 2: {sb}");
		}

		/// <summary>
		/// Replace entries of type Yomi.
		/// </summary>
		private void TranslateStageThree(StringBuilder sb)
		{
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Yomi)) LogReplace(sb, entry.Input, entry.Output, entry.Id);
            StaticHelpers.LogVerbose($"Stage 3: {sb}");
		}

		public string ReplaceNames(string original)
		{
			var sb = new StringBuilder(original);
			foreach (var entry in _entries.Where(x => x.Type == EntryType.Name || x.Type == EntryType.Yomi))
			{
				sb.Replace(entry.Input, entry.Output);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Replace entries of type Name and translation to proxies.
		/// </summary>
		private IEnumerable<Entry> TranslateStageFour(StringBuilder sb, HappyReaderDatabase data)
		{
			var roles = _entries.Select(z => z.RoleString).Distinct().ToArray();
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
			var entriesWithProxiesArray = _entries.Where(i => (i.Type == EntryType.Name || i.Type == EntryType.Translation) && !i.Input.Contains("[[")).OrderByDescending(x => x.Input.Length).ToArray();
			var usefulEntriesWithProxies = entriesWithProxiesArray.OrderByDescending(x => x.Input.Length).ToList();
			//replace these with initial proxies
			foreach (var entry in entriesWithProxiesArray)
			{
				if (!sb.ToString().Contains(entry.Input))
				{
					usefulEntriesWithProxies.Remove(entry);
					continue;
				}
				var roleString = entry.RoleString ?? (entry.Type == EntryType.Name ? "m" : "n");
				var proxy = proxies[roleString].Proxies.Count == 0 ? null : proxies[roleString].Proxies.Dequeue();
				proxies[roleString].Count++;
				if (proxy == null)
				{
					StaticHelpers.LogToFile("No proxy available, stopping translate.");
					throw new Exception("Error - no proxy available.");
				}
				proxy.FullRoleString = $"[[{roleString}#{proxies[roleString].Count}]]";
				proxy.Id = proxies[roleString].Count;
				entry.AssignedProxy = proxy;
				LogReplace(sb, entry.Input, entry.AssignedProxy.FullRoleString, entry.Id);
			}
			StaticHelpers.LogVerbose($"Stage 4.0: {sb}");
			//perform replaces involving proxies
			var entriesOnProxies = _entries.Where(i => i.Type == EntryType.ProxyMod).ToArray();
			TranslateStage4P1(sb, usefulEntriesWithProxies, entriesOnProxies);
			foreach (var entry in usefulEntriesWithProxies)
			{
				LogReplace(sb, entry.AssignedProxy.FullRoleString, entry.AssignedProxy.Entry.Input, entry.Id);
			}
			StaticHelpers.LogVerbose($"Stage 4.2: {sb}");
			return usefulEntriesWithProxies;
		}

		private void TranslateStage4P1(StringBuilder sb, IReadOnlyCollection<Entry> entriesWithProxies, IEnumerable<Entry> entriesOnProxies)
		{
			foreach (var entry in entriesOnProxies)
			{
				var input = Regex.Replace(entry.Input, @"\[\[(.+?)]]", @"\[\[$1#(\d+)]]");
				var matches = Regex.Matches(sb.ToString(), input).Cast<Match>().Select(x=>Int32.Parse(x.Groups[1].Value)).Distinct();
				foreach (int match in matches)
				{
					entriesWithProxies.Single(x => x.AssignedProxy.Id == match).AssignedProxy.ProxyMods.Add(entry);
				}
				var output = new Regex(@"^.*?\[\[(.+)]].*?$").Replace(entry.Output, @"[[$1#$$1]]");
				LogReplaceRegex(sb, input, output, entry.Id);
			}
			StaticHelpers.LogVerbose($"Stage 4.1: {sb}");
		}

		public string[] Translate(string input)
		{
			var result = new string[8];
			if (input.Length == 1)
			{
				var kana = Kakasi.JapaneseToKana(input);
				if (kana == input)
				{
					var romaji = Kakasi.JapaneseToRomaji(input);
					result[0] = input;
					result[1] = input;
					result[2] = input;
					result[3] = input;
					result[4] = input;
					result[5] = romaji;
					result[6] = romaji;
					result[7] = romaji;
					return result;
				}
			}
			if (input.Length == 0)
			{

			}
			var sb = new StringBuilder(input);
			//process in stages
            StaticHelpers.LogVerbose($"Stage 0: {sb}");
			result[0] = sb.ToString();
			TranslateStageOne(sb);
			result[1] = sb.ToString();
			TranslateStageTwo(sb);
			result[2] = sb.ToString();
			TranslateStageThree(sb);
			result[3] = sb.ToString();
			IEnumerable<Entry> usefulEntriesWithProxies;
			try
			{
				usefulEntriesWithProxies = TranslateStageFour(sb, _data);
			}
			catch (Exception ex)
			{
				throw new Exception("Error in TranslateStageFour, see inner", ex);
			}
			result[4] = sb.ToString();
			GoogleTranslate.Translate(sb);
            StaticHelpers.LogVerbose($"Stage 5: {sb}");
			result[5] = sb.ToString();
			TranslateStageSix(sb, usefulEntriesWithProxies);
			result[6] = sb.ToString();
			TranslateStageSeven(sb);
			result[7] = sb.ToString();
			return result;
		}

		/// <summary>
		/// Replace Name and Translation proxies to entry outputs.
		/// </summary>
		private static void TranslateStageSix(StringBuilder sb, IEnumerable<Entry> usefulEntriesWithProxies)
		{
			foreach (var entry in usefulEntriesWithProxies)
			{
				LogReplace(sb, entry.AssignedProxy.Entry.Output, entry.AssignedProxy.FullRoleString, entry.Id);
				foreach (var proxyMod in entry.AssignedProxy.ProxyMods)
				{
					var pmO = proxyMod.Output.Replace($"[[{proxyMod.RoleString ?? "m"}]]", entry.AssignedProxy.FullRoleString);
					LogReplace(sb, entry.AssignedProxy.FullRoleString, pmO, proxyMod.Id);
				}
				LogReplace(sb, entry.AssignedProxy.FullRoleString, entry.Output, entry.Id);
			}
            StaticHelpers.LogVerbose($"Stage 6: {sb}");
		}

		/// <summary>
		/// Replace entries of type Output.
		/// </summary>
		private void TranslateStageSeven(StringBuilder sb)
		{
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Output && !i.Regex)) LogReplace(sb, entry.Input, entry.Output, entry.Id);
			foreach (var entry in _entries.Where(i => i.Type == EntryType.Output && i.Regex)) LogReplaceRegex(sb, entry.Input, entry.Output, entry.Id);
            StaticHelpers.LogVerbose($"Stage 7: {sb}");
		}
		
		private static string CheckRepeatedString(string text)
		{
			//check if repeated after name
			var firstBracket = text.IndexOfAny(new[] { '「', '『' });
			if (firstBracket > 0)
			{
				var name = text.Substring(0, firstBracket);
				var checkText = text.Substring(firstBracket);
				return name + ReduceRepeatedString(checkText);
			}
			return ReduceRepeatedString(text);
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
                Debug.WriteLine($"Replace happened - id {id}: '{input}' > '{output}'");
            }
#else
			sb.Replace(input, output);
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// ReSharper disable once UnusedParameter.Local
		private static void LogReplaceRegex(StringBuilder sb, string input, string output, long id)
		{
			var rgx = new Regex(input);
#if LOGVERBOSE
            var sbOriginal = sb.ToString();
            var sbReplaced = rgx.Replace(sbOriginal, output);
            if (sbOriginal != sbReplaced)
            {
                Debug.WriteLine($"Replace happened - id {id}: '{input}' > '{output}'");
            }
#else
			var sbReplaced = rgx.Replace(sb.ToString(), output);
#endif
			sb.Clear();
			sb.Append(sbReplaced);
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
