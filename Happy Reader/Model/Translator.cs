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

namespace Happy_Reader
{
    internal static class Translator
    {
        //TODO add regex to all stages

        private static readonly object TranslateLock = new object();
        private static readonly HappyReaderDatabase Data = new HappyReaderDatabase();

        private static User _lastUser;
        private static ListedVN _lastGame;
        private static Entry[] _entries;
        public static bool RefreshEntries;
        private static readonly char[] Separators = "『「」』…".ToCharArray();
        private static readonly char[] InclusiveSeparators = "。？".ToCharArray();
        private static readonly char[] AllSeparators = Separators.Concat(InclusiveSeparators).ToArray();
        
        public static Translation Translate(User user, ListedVN game, string input)
        {
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

        private static void SetEntries(User user, ListedVN game)
        {
            RefreshEntries = false;
            _lastUser = user;
            _lastGame = game;
            int[] gamesInSeries = null;
            if (game != null)
            {
                gamesInSeries = game.Series == null
                    ? new[] { game.VNID }
                    : StaticHelpers.LocalDatabase.VisualNovels.Where(g => g.Series == game.Series).Select(gg => gg.VNID).ToArray();
            }
            Entry[] generalEntries;
            if (user == null)
            {
                generalEntries =
                    Data.Entries.Where(e =>
                            !e.Private &&
                            !e.SeriesSpecific).ToArray();
            }
            else
            {
                generalEntries =
                    Data.Entries.Where(e =>
                            (e.Private && e.UserId == user.Id || !e.Private) &&
                            !e.SeriesSpecific).ToArray();
            }
            Entry[] specificEntries = { };
            if (user == null && gamesInSeries != null)
            {
                specificEntries = Data.Entries.Where(e =>
                            !e.Private &&
                            e.SeriesSpecific && gamesInSeries.Contains(e.GameId.Value)).ToArray(); //todo make GameId int
            }
            else if (gamesInSeries != null)
            {
                specificEntries = Data.Entries.Where(e =>
                        (e.Private && e.UserId == user.Id || !e.Private) &&
                        e.SeriesSpecific && gamesInSeries.Contains(e.GameId.Value)).ToArray();
            }
            _entries = generalEntries.Concat(specificEntries).OrderBy(i => i.Id).ToArray();
            Debug.WriteLine($"General entries: {generalEntries.Length}. Specific entries: {specificEntries.Length}");
        }

        /// <summary>
        /// Replace entries of type Game.
        /// </summary>
        public static void TranslateStageOne(StringBuilder sb)
        {
            foreach (var entry in _entries.Where(i => i.Type == EntryType.Game))
            {
                if (entry.Regex) sb.LogReplaceRegex(entry.Input, entry.Output, entry.Id);
                else sb.LogReplace(entry.Input, entry.Output, entry.Id);
            }
#if LOGVERBOSE
            Debug.WriteLine($"Stage 1: {sb}");
#endif
        }

        /// <summary>
        /// Replace entries of type Input.
        /// </summary>
        private static void TranslateStageTwo(StringBuilder sb)
        {
            foreach (var entry in _entries.Where(i => i.Type == EntryType.Input)) sb.LogReplace(entry.Input, entry.Output, entry.Id);
#if LOGVERBOSE
            Debug.WriteLine($"Stage 2: {sb}");
#endif
        }

        /// <summary>
        /// Replace entries of type Yomi.
        /// </summary>
        private static void TranslateStageThree(StringBuilder sb)
        {
            foreach (var entry in _entries.Where(i => i.Type == EntryType.Yomi)) sb.LogReplace(entry.Input, entry.Output, entry.Id);
#if LOGVERBOSE
            Debug.WriteLine($"Stage 3: {sb}");
#endif
        }

        public static string ReplaceNames(string original)
        {
            var sb = new StringBuilder(original);
            foreach (var entry in _entries.Where(x=>x.Type == EntryType.Name || x.Type == EntryType.Yomi))
            {
                sb.Replace(entry.Input, entry.Output);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Replace entries of type Name and translation to proxies.
        /// </summary>
        private static IEnumerable<Entry> TranslateStageFour(StringBuilder sb, HappyReaderDatabase data)
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
            var entriesWithProxiesArray = _entries.Where(i => (i.Type == EntryType.Name || i.Type == EntryType.Translation) && !i.Input.Contains("[[")).ToArray();
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
                sb.LogReplace(entry.Input, entry.AssignedProxy.FullRoleString, entry.Id);
            }
#if LOGVERBOSE
            Debug.WriteLine($"Stage 4.0: {sb}");
#endif
            //perform replaces involving proxies
            var entriesOnProxies = _entries.Where(i => i.Type == EntryType.ProxyMod).ToArray();
            TranslateStage4P1(sb, usefulEntriesWithProxies, entriesOnProxies);
            foreach (var entry in usefulEntriesWithProxies)
            {
                sb.LogReplace(entry.AssignedProxy.FullRoleString, entry.AssignedProxy.Entry.Input, entry.Id);
            }
#if LOGVERBOSE
            Debug.WriteLine($"Stage 4.2: {sb}");
#endif
            return usefulEntriesWithProxies;
        }

        private static void TranslateStage4P1(StringBuilder sb, List<Entry> entriesWithProxies, Entry[] entriesOnProxies)
        {
            foreach (var entry in entriesOnProxies)
            {
                var input = Regex.Replace(entry.Input, @"\[\[(.+?)]]", @"\[\[$1#(\d+)]]");
                var matches = Regex.Matches(sb.ToString(), input);
                foreach (Match match in matches)
                {
                    entriesWithProxies.Single(x => x.AssignedProxy.Id == int.Parse(match.Groups[1].Value)).AssignedProxy.ProxyMods.Add(entry);
                }
                var output = new Regex(@"^.*?\[\[(.+)]].*?$").Replace(entry.Output, @"[[$1#$$1]]");
                sb.LogReplaceRegex(input, output, entry.Id);
            }
#if LOGVERBOSE
            Debug.WriteLine($"Stage 4.1: {sb}");
#endif
        }

        public static string[] Translate(string input)
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
#if LOGVERBOSE
            Debug.WriteLine($"Stage 0: {sb}");
#endif
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
                usefulEntriesWithProxies = TranslateStageFour(sb, Data);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in TranslateStageFour, see inner", ex);
            }
            result[4] = sb.ToString();
            GoogleTranslate.Translate(sb);
#if LOGVERBOSE
            Debug.WriteLine($"Stage 5: {sb}");
#endif
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
                sb.LogReplace(entry.AssignedProxy.Entry.Output, entry.AssignedProxy.FullRoleString, entry.Id);
                foreach (var proxyMod in entry.AssignedProxy.ProxyMods)
                {
                    var pmO = proxyMod.Output.Replace($"[[{proxyMod.RoleString ?? "m"}]]", entry.AssignedProxy.FullRoleString);
                    sb.LogReplace(entry.AssignedProxy.FullRoleString, pmO, proxyMod.Id);
                }
                sb.LogReplace(entry.AssignedProxy.FullRoleString, entry.Output, entry.Id);
            }
#if LOGVERBOSE
            Debug.WriteLine($"Stage 6: {sb}");
#endif
        }

        /// <summary>
        /// Replace entries of type Output.
        /// </summary>
        private static void TranslateStageSeven(StringBuilder sb)
        {
            foreach (var entry in _entries.Where(i => i.Type == EntryType.Output && !i.Regex)) sb.LogReplace(entry.Input, entry.Output, entry.Id);
            foreach (var entry in _entries.Where(i => i.Type == EntryType.Output && i.Regex)) sb.LogReplaceRegex(entry.Input, entry.Output, entry.Id);
#if LOGVERBOSE
            Debug.WriteLine($"Stage 7: {sb}");
#endif
        }

        /// <summary>
        /// Loads cache with passed collection.
        /// </summary>
        public static void LoadTranslationCache(IEnumerable<HRGoogleTranslate.Translation> cachedTranslations)
        {
            GoogleTranslate.LoadCache(cachedTranslations);
        }

        /// <summary>
        /// Gets current cache in Translator.
        /// </summary>
        public static IEnumerable<HRGoogleTranslate.Translation> GetNewCache()
        {
            return GoogleTranslate.GetNewCache();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogReplace(this StringBuilder sb, string input, string output, long id)
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
        private static void LogReplaceRegex(this StringBuilder sb, string input, string output, long id)
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
