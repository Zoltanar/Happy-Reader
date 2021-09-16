using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Happy_Apps_Core;
using Happy_Reader.Database;

namespace Happy_Reader.TranslationEngine
{
	public partial class Translator
	{
		/// <summary>
		/// Replace entries of type Name and Translation to proxies.
		/// </summary>
		private List<Entry> TranslateStageFour(StringBuilder sb, HappyReaderDatabase data, TranslationResults result)
		{
			result.SetStage(4);
			var usefulEntriesWithProxies = GetRelevantEntriesWithProxies(sb, data, out Dictionary<string, ProxiesWithCount> proxies);
			if (usefulEntriesWithProxies.Count != 0)
			{
				foreach (var entry in usefulEntriesWithProxies)
				{
					var proxyAssigned = AssignProxy(proxies, entry);
					if (proxyAssigned)
					{
						if (entry.Regex) LogReplaceRegex(sb, entry.Input, entry.AssignedProxy.FullRoleString, result, entry);
						else LogReplace(sb, entry.Input, entry.AssignedProxy.FullRoleString, result, entry);
					}
				}
				usefulEntriesWithProxies = usefulEntriesWithProxies.Where(e => e.AssignedProxy != null).ToList();
				StaticHelpers.Logger.Verbose($"Stage 4.0: {sb}");
				//perform replaces involving proxies
				var entriesOnProxies = OrderEntries(_entries.Where(i => i.Type == EntryType.ProxyMod)).ToList();
				TranslateStage4P1(sb, usefulEntriesWithProxies, entriesOnProxies, result, proxies);
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
					var proxyModOutput = proxyMod.Output.Replace($"[[{proxyMod.RoleString ?? "m"}]]", entry.AssignedProxy.FullRoleString);
					LogReplace(sb, entry.AssignedProxy.FullRoleString, proxyModOutput, result, entry);
				}
				LogReplace(sb, entry.AssignedProxy.FullRoleString, entry.Output, result, entry);
			}
			StaticHelpers.Logger.Verbose($"Stage 6: {sb}");
			result[6] = sb.ToString();
		}

		private static bool AssignProxy(IReadOnlyDictionary<string, ProxiesWithCount> proxies, Entry entry)
		{
			var proxyParts = entry.RoleString.Split('.');
			var mainProxy = proxyParts[0];
			var proxy = proxies[entry.RoleString].Proxies.Count == 0 ? null : proxies[entry.RoleString].Proxies.Dequeue();
			if (proxy == null && proxyParts.Any()) proxy = proxies[mainProxy].Proxies.Count == 0 ? null : proxies[mainProxy].Proxies.Dequeue();
			proxies[mainProxy].Count++;
			if (proxy == null)
			{
				StaticHelpers.Logger.ToFile($"No proxy available ({entry.RoleString}).");
				throw new Exception($"Error - no proxy available ({entry.RoleString}).");
			}
			proxy.MainRole = mainProxy;
			proxy.Id = proxies[mainProxy].Count;
			entry.AssignedProxy = proxy;
			return true;
		}

		/// <summary>
		/// Resolve ProxyMods, including merges, and replace them with proxies.
		/// </summary>
		private void TranslateStage4P1(
			StringBuilder sb,
			ICollection<Entry> entriesWithProxies,
			ICollection<Entry> entriesOnProxies,
			TranslationResults result,
			Dictionary<string, ProxiesWithCount> proxies)
		{
			bool matchFound;
			int loopCount = 0;
			do
			{
				matchFound = false;
				loopCount++;
				foreach (var entry in entriesOnProxies)
				{
					var input = Stage4P1InputRegex.Replace(entry.Input, @"\[\[$1#(\d+)]]");
					var matches = Regex.Matches(sb.ToString(), input).Cast<Match>().ToList();
					var roleGroups = matches.SelectMany(x => x.Groups.Cast<Group>().Skip(1).Select(g => int.Parse(g.Value))).Distinct().ToList();
					if (matches.Count == 0) continue;
					var merge = matches.Count == 1 && roleGroups.Count > 1;
					var mergedEntry = new Entry
					{
						RoleString = entry.RoleString,
						Input = input,
						Output = entry.Output
					};
					foreach (int match in roleGroups)
					{
						var matchedEntry = entriesWithProxies.Single(x =>
						{
							var mainProxyPart = x.AssignedProxy.Role.Split('.')[0];
							return x.AssignedProxy.Id == match && mainProxyPart == entry.RoleString;
						});
						if (merge)
						{
							if (mergedEntry.AssignedProxy == null)
							{
								AssignProxy(proxies, mergedEntry);
							}
							Debug.Assert(mergedEntry.AssignedProxy != null, "mergedEntry.AssignedProxy != null");
							mergedEntry.AssignedProxy.ProxyMods.AddRange(matchedEntry.AssignedProxy.ProxyMods);
							var mergedPattern = new Regex($@"\[\[([^];]+?)#{matchedEntry.AssignedProxy.Id}]]");
							mergedEntry.Output = mergedPattern.Replace(mergedEntry.Output, matchedEntry.Output);
						}
						else matchedEntry.AssignedProxy.ProxyMods.Add(entry);
					}
					string output;
					if (merge && roleGroups.Count > 1)
					{
						entriesWithProxies.Add(mergedEntry);
						output = mergedEntry.AssignedProxy.FullRoleString;
					}
					else output = Stage4P1OutputRegex.Replace(entry.Output, @"[[$1#$$1]]");
					LogReplaceRegex(sb, input, output, result, entry);
					matchFound = true;
				}
				//something could have gone wrong causing infinite loop
				if (loopCount > 100) break;
			}
			while (matchFound);
			StaticHelpers.Logger.Verbose($"Stage 4.1: {sb}");
		}

		private List<Entry> GetRelevantEntriesWithProxies(StringBuilder sb, HappyReaderDatabase data, out Dictionary<string, ProxiesWithCount> proxies)
		{
			var roles = _entries.Select(z => z.RoleString).Distinct().ToArray();
			proxies = BuildProxiesList(data, roles);
			var entriesWithProxiesArray = OrderEntries(_entries.Where(i => i.Type == EntryType.Name || i.Type == EntryType.Translation)).ToArray();
			var usefulEntriesWithProxies = RemoveUnusedEntriesAndSetLocation(sb, entriesWithProxiesArray);
			if (usefulEntriesWithProxies.Count == 0) return usefulEntriesWithProxies;
			RemoveSubEntries(usefulEntriesWithProxies);
			MergeNeighbouringEntries(usefulEntriesWithProxies);
			return usefulEntriesWithProxies;
		}
		
		private static Dictionary<string, ProxiesWithCount> BuildProxiesList(HappyReaderDatabase data, IEnumerable<string> roles)
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
	}

	internal class ProxiesWithCount
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
