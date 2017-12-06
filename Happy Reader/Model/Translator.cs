using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using Happy_Reader.Database;
using static Happy_Reader.StaticMethods;
#if LOGVERBOSE
using System.Diagnostics;
#endif
using OriginalTextObject = System.Collections.Generic.List<(string Original, string Romaji)>;

namespace Happy_Reader
{
    internal static class Translator
    {
        //TODO add regex to all stages

        private static readonly object TranslateLock = new object();//("translate-lock-123");
        private static readonly HappyReaderDatabase Data = new HappyReaderDatabase();
        
        public static string[] TestTranslate(User user, Game game, string input, out OriginalTextObject originalText)
        {
            var result = new string[8];
            //Debug.WriteLine($"'{input}' > 'Debug: Not translating.'");
            //return "Debug: Not translating.";
            lock (TranslateLock)
            {
                var sb = new StringBuilder(input);
                long[] gamesInSeries = null;
                if (game != null)
                {
                    gamesInSeries = game.Series == null
                        ? new[] { game.Id }
                        : Data.Games.Where(g => g.Series == game.Series).Select(gg => gg.Id).ToArray();
                }
                Entry[] generalEntries;
                if (user == null)
                {
                    generalEntries =
                        Data.Entries.Where(e =>
                                //entry is not private
                                !e.Private &&
                                //entry is not series specific
                                !e.SeriesSpecific).ToArray();
                }
                else
                {
                    generalEntries =
                        Data.Entries.Where(e =>
                                //entry is not private
                                (e.Private && e.UserId == user.Id || !e.Private) &&
                                //entry is not series specific
                                !e.SeriesSpecific).ToArray();
                }
                Entry[] specificEntries = { };
                if (user == null && gamesInSeries != null)
                {
                    specificEntries = Data.Entries.Where(e =>
                                //entry is not private
                                !e.Private &&
                                //entry is series specific and is for a game in series
                                e.SeriesSpecific && gamesInSeries.Contains(e.GameId.Value)).ToArray();
                }
                else if (gamesInSeries != null)
                {
                    specificEntries = Data.Entries.Where(e =>
                            //entry is not private
                            (e.Private && e.UserId == user.Id || !e.Private) &&
                            //entry is series specific and is for a game in series
                            e.SeriesSpecific && gamesInSeries.Contains(e.GameId.Value)).ToArray();
                }
                var entries = generalEntries.Concat(specificEntries).OrderBy(i => i.Id).ToArray();
                Debug.WriteLine(
                    $"General entries: {generalEntries.Length}. Specific entries: {specificEntries.Length}");
                //process in stages
#if LOGVERBOSE
                Debug.WriteLine($"Stage 0: {sb}");
#endif
                result[0] = sb.ToString();
                TranslateStageOne(sb, entries);
                result[1] = sb.ToString();
                TranslateStageTwo(sb, entries);
                result[2] = sb.ToString();
                originalText = GetRomaji(sb);
                TranslateStageThree(sb, entries);
                result[3] = sb.ToString();
                IEnumerable<Entry> usefulEntriesWithProxies;
                try
                {
                    usefulEntriesWithProxies = TranslateStageFour(sb, entries, Data);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error in TranslateStageFour, see inner", ex);
                }
                result[4] = sb.ToString();
                HRGoogleTranslate.GoogleTranslate.Translate(sb);
#if LOGVERBOSE
                Debug.WriteLine($"Stage 5: {sb}");
#endif
                result[5] = sb.ToString();
                TranslateStageSix(sb, usefulEntriesWithProxies);
                result[6] = sb.ToString();
                TranslateStageSeven(sb, entries);
                result[7] = sb.ToString();
                return result;
            }
        }

        private const string RomajiConverterUrl = "http://www.kawa.net/works/ajax/romanize/romanize.cgi";

        public static OriginalTextObject GetRomaji(StringBuilder sb)
        {
            var pairs = new List<(string Original, string Romaji)>();
            var request = WebRequest.Create($"{RomajiConverterUrl}?mode=japanese&ie=UTF-8&q={WebUtility.UrlEncode(sb.ToString())}");
            request.Method = "GET";
            var response = request.GetResponse();
            string romaji;
            using (var stream = response.GetResponseStream())
            {
                if (stream == null)
                {
                    pairs.Add(("Failed to get romaji", null));
                    return pairs;
                }

                var reader = new StreamReader(stream);
                romaji = reader.ReadToEnd();
            }
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(romaji);
            var nodes = xmlDoc.SelectNodes("ul/li/span");
            if (nodes == null)
            {
                pairs.Add(("Failed to get romaji", null));
                return pairs;
            }
            foreach (XmlNode node in nodes)
            {
                pairs.Add((node.InnerText, node.Attributes?["title"]?.Value));
            }
            return pairs;
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
                var proxy = proxies[roleString].Proxies.Count == 0 ? default(RoleProxy) : proxies[roleString].Proxies.Dequeue();//proxies.FirstOrDefault(p => p.Role == roleString);
                proxies[roleString].Count++;
                if (proxy.Equals(default(RoleProxy)))
                {
                    LogToConsole("No proxy available, stopping translate.");
                    throw new Exception("Error - no proxy available.");
                }
                proxy.FullRoleString = $"[[{roleString}#{proxies[roleString].Count}]]";
                entry.AssignedProxy = proxy;
                sb.LogReplace(entry.Input, entry.AssignedProxy.FullRoleString, entry.Id);
            }
#if LOGVERBOSE
            Debug.WriteLine($"Stage 4.0: {sb}");
#endif
            //perform replaces involving proxies
            var entriesOnProxies = entries.Where(i => i.Type == EntryType.ProxyMod).ToArray();
            foreach (var entry in entriesOnProxies)
            {
                sb.LogReplace(entry.Input, entry.Output, entry.Id);
            }
#if LOGVERBOSE
            Debug.WriteLine($"Stage 4.1: {sb}");
#endif
            foreach (var entry in usefulEntriesWithProxies)
            {
                sb.LogReplace(entry.AssignedProxy.FullRoleString, entry.AssignedProxy.Entry.Input, entry.Id);
            }
#if LOGVERBOSE
            Debug.WriteLine($"Stage 4.2: {sb}");
#endif
            return usefulEntriesWithProxies;
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

        /// <summary>
        /// Replace Name and Translation proxies to entry outputs.
        /// </summary>
        private static void TranslateStageSix(StringBuilder sb, IEnumerable<Entry> usefulEntriesWithProxies)
        {
            foreach (var entry in usefulEntriesWithProxies)
            {
                sb.LogReplace(entry.AssignedProxy.Entry.Output, entry.Output, entry.Id);
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


    }
}
