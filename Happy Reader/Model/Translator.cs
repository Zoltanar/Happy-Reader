using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using HRGoogleTranslate;
using static Happy_Reader.StaticMethods;
using System.Diagnostics;
using OriginalTextObject = System.Collections.Generic.List<(string Original, string Romaji)>;

namespace Happy_Reader
{
    internal static class Translator
    {
        //TODO add regex to all stages

        private static readonly object TranslateLock = new object();
        private static readonly HappyReaderDatabase Data = new HappyReaderDatabase();

        private static User _lastUser;
        private static ListedVN _lastGame;
        private static Entry[] _lastEntries;
        public static bool RefreshEntries;
        private static readonly char[] Separators = "『「」』…".ToCharArray();
        private static readonly char[] InclusiveSeparators = "。？".ToCharArray();
        private static readonly char[] AllSeparators = Separators.Concat(InclusiveSeparators).ToArray();

        //todo make it to use a single object
        public static string[] Translate(User user, ListedVN game, string input, out OriginalTextObject originalText)
        {
            //Debug.WriteLine($"'{input}' > 'Debug: Not translating.'");
            //return "Debug: Not translating.";
            lock (TranslateLock)
            {
                var item = new TranslationObject();
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
                if (user != _lastUser || game != _lastGame || RefreshEntries) SetEntries(user, game);
                item.TranslateParts();
                originalText = item.Original;
                return item.Results;
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
            _lastEntries = generalEntries.Concat(specificEntries).OrderBy(i => i.Id).ToArray();
            Debug.WriteLine($"General entries: {generalEntries.Length}. Specific entries: {specificEntries.Length}");
        }

        /// <summary>
        /// Replace entries of type Game.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="entries"></param>
        private static void TranslateStageOne(StringBuilder sb, Entry[] entries)
        {
            foreach (var entry in entries.Where(i => i.Type == EntryType.Game)) sb.LogReplace(entry.Input, entry.Output, entry.Id);
#if LOGVERBOSE
            Debug.WriteLine($"Stage 1: {sb}");
#endif
        }

        /// <summary>
        /// Replace entries of type Input.
        /// </summary>
        private static void TranslateStageTwo(StringBuilder sb, Entry[] entries)
        {
            foreach (var entry in entries.Where(i => i.Type == EntryType.Input)) sb.LogReplace(entry.Input, entry.Output, entry.Id);
#if LOGVERBOSE
            Debug.WriteLine($"Stage 2: {sb}");
#endif
        }

        /// <summary>
        /// Replace entries of type Yomi.
        /// </summary>
        private static void TranslateStageThree(StringBuilder sb, Entry[] entries)
        {
            foreach (var entry in entries.Where(i => i.Type == EntryType.Yomi)) sb.LogReplace(entry.Input, entry.Output, entry.Id);
#if LOGVERBOSE
            Debug.WriteLine($"Stage 3: {sb}");
#endif
        }

        /// <summary>
        /// Replace entries of type Name and translation to proxies.
        /// </summary>
        private static IEnumerable<Entry> TranslateStageFour(StringBuilder sb, Entry[] entries, HappyReaderDatabase data)
        {
            var roles = entries.Select(z => z.RoleString).Distinct().ToArray();
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
            var entriesWithProxiesArray = entries.Where(i => (i.Type == EntryType.Name || i.Type == EntryType.Translation) && !i.Input.Contains("[[")).ToArray();
            var usefulEntriesWithProxies = entriesWithProxiesArray.ToList();
            //replace these with initial proxies
            foreach (var entry in entriesWithProxiesArray)
            {
                if (!sb.ToString().Contains(entry.Input))
                {
                    usefulEntriesWithProxies.Remove(entry);
                    continue;
                }
                var roleString = entry.RoleString ?? (entry.Type == EntryType.Name ? "m" : "n"); //m default for name, n default for translation
                var proxy = proxies[roleString].Proxies.Count == 0 ? null : proxies[roleString].Proxies.Dequeue();//proxies.FirstOrDefault(p => p.Role == roleString);
                proxies[roleString].Count++;
                if (proxy == null)
                {
                    LogToConsole("No proxy available, stopping translate.");
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
            var entriesOnProxies = entries.Where(i => i.Type == EntryType.ProxyMod).ToArray();
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

        private static string[] Translate(string input, out OriginalTextObject originalText)
        {
            var result = new string[8];
            var sb = new StringBuilder(input);
            //process in stages
#if LOGVERBOSE
            Debug.WriteLine($"Stage 0: {sb}");
#endif
            result[0] = sb.ToString();
            TranslateStageOne(sb, _lastEntries);
            result[1] = sb.ToString();
            TranslateStageTwo(sb, _lastEntries);
            result[2] = sb.ToString();
            originalText = new OriginalTextObject();
            var romaji = Kakasi.JapaneseToRomaji(sb.ToString());
            var kana = Kakasi.JapaneseToKana(sb.ToString());
            var o = kana.Split(' ').Zip(romaji.Split(' '), (x, y) => (x, y)).ToArray();
            originalText.AddRange(o);
            TranslateStageThree(sb, _lastEntries);
            result[3] = sb.ToString();
            IEnumerable<Entry> usefulEntriesWithProxies;
            try
            {
                usefulEntriesWithProxies = TranslateStageFour(sb, _lastEntries, Data);
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
            TranslateStageSeven(sb, _lastEntries);
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
                    var pmO = proxyMod.Output.Replace($"[[{proxyMod.RoleString}]]", entry.AssignedProxy.FullRoleString);
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
        private static void TranslateStageSeven(StringBuilder sb, Entry[] entries)
        {
            foreach (var entry in entries.Where(i => i.Type == EntryType.Output && !i.Regex)) sb.LogReplace(entry.Input, entry.Output, entry.Id);
            foreach (var entry in entries.Where(i => i.Type == EntryType.Output && i.Regex)) sb.LogReplaceRegex(entry.Input, entry.Output, entry.Id);
#if LOGVERBOSE
            Debug.WriteLine($"Stage 7: {sb}");
#endif
        }

        /// <summary>
        /// Loads cache with passed collection.
        /// </summary>
        public static void LoadTranslationCache(IEnumerable<Translation> cachedTranslations)
        {
            GoogleTranslate.LoadCache(cachedTranslations);
        }

        /// <summary>
        /// Gets current cache in Translator.
        /// </summary>
        public static IEnumerable<Translation> GetCache()
        {
            return GoogleTranslate.GetCache();
        }

        private class TranslationObject
        {
            private readonly List<OriginalTextObject> _partOriginals = new List<OriginalTextObject>();
            internal readonly List<(string Part, bool Translate)> Parts = new List<(string Part, bool Translate)>();
            private readonly List<string[]> _partResults = new List<string[]>();
            public readonly OriginalTextObject Original = new OriginalTextObject();
            public readonly string[] Results = new string[8];
            
            public void TranslateParts()
            {
                try
                {
                    foreach (var item in Parts)
                    {
                        if (!item.Translate)
                        {
                            _partResults.Add(Enumerable.Repeat(item.Part, 8).ToArray());
                            _partOriginals.Add(new OriginalTextObject { (item.Part, null) });
                            continue;
                        }
                        _partResults.Add(Translate(item.Part, out OriginalTextObject original));
                        _partOriginals.Add(original);
                    }
                    for (int stage = 0; stage < 8; stage++)
                    {
                        var stage1 = stage;
                        Results[stage] = string.Join("", _partResults.Select(c => c[stage1]));
                    }
                    foreach (OriginalTextObject originalPart in _partOriginals)
                    {
                        originalPart.ForEach(Original.Add);
                    }
                }
                catch (Exception ex)
                {
                    LogToConsole(ex.Message); //todo log to file
                }

            }
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
