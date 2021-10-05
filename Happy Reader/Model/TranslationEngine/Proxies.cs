using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
			if (usefulEntriesWithProxies.Count == 0)
			{
				StaticHelpers.Logger.Verbose($"Stage 4.2: {sb}");
				result[4] = sb.ToString();
				return usefulEntriesWithProxies;
			}
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
			foreach (var value in proxies.Values)
			{
				value.Used = value.Count = 0;
			}
			foreach (var entry in usefulEntriesWithProxies.ToList())
			{
				if (!sb.ToString().Contains(entry.AssignedProxy.FullRoleString))
				{
					usefulEntriesWithProxies.Remove(entry);
					continue;
				}
				var inputRoleString = entry.AssignedProxy.FullRoleString;
				AssignProxy(proxies, entry, true);
				foreach (var proxyMod in entry.AssignedProxy.ProxyMods) result.AddEntryUsed(proxyMod);
				LogReplace(sb, inputRoleString, entry.AssignedProxy.Entry.Input, result, entry);
			}
			StaticHelpers.Logger.Verbose($"Stage 4.2: {sb}");
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

		private static bool AssignProxy(IReadOnlyDictionary<string, ProxiesWithCount> proxies, Entry entry, bool dequeue = false)
		{
			var proxyParts = entry.RoleString.Split('.');
			var mainProxy = proxyParts[0];
			var proxy = GetProxy(entry.RoleString);
			if (proxy == null && proxyParts.Any()) proxy = GetProxy(mainProxy);
			proxies[mainProxy].Count++;
			if (proxy == null && dequeue)
			{
				StaticHelpers.Logger.ToFile($"No proxy available ({entry.RoleString}).");
				throw new Exception($"Error - no proxy available ({entry.RoleString}).");
			}
			if (proxy == null)
			{
				proxy = new RoleProxy
				{
					MainRole = mainProxy,
					Id = proxies[mainProxy].Count
				};
			}
			else
			{
				proxy.MainRole = mainProxy;
				proxy.Id = proxies[mainProxy].Count;
			}
			if (dequeue)
			{
				proxy.Id = entry.AssignedProxy.Id;
				proxy.ProxyMods.AddRange(entry.AssignedProxy.ProxyMods);
				entry.AssignedProxy.ProxyMods.Clear();
			}
			entry.AssignedProxy = proxy;
			return true;

			RoleProxy GetProxy(string roleString)
			{
				var proxiesWithCount = proxies[roleString];
				if (proxiesWithCount.Proxies.Count == 0) return null;
				RoleProxy result;
				if (dequeue)
				{
					result = proxiesWithCount.Proxies.Dequeue();
				}
				else
				{
					result = proxiesWithCount.Proxies.Count < proxiesWithCount.Used + 1
						? null
						: proxiesWithCount.Proxies.ElementAt(proxiesWithCount.Used++);
				}

				return result;
			}
		}

		/// <summary>
		/// Resolve ProxyMods, including merges, and replace them with proxies.
		/// </summary>
		private void TranslateStage4P1(
			StringBuilder sb,
			ICollection<Entry> entriesWithProxies,
			ICollection<Entry> proxyMods,
			TranslationResults result,
			Dictionary<string, ProxiesWithCount> proxies)
		{
			bool matchFound;
			int loopCount = 0;
			do
			{
				matchFound = false;
				loopCount++;
				foreach (var entry in proxyMods)
				{
					ResolveProxyMod(sb, entriesWithProxies, result, proxies, entry, ref matchFound);
				}
				//something could have gone wrong causing infinite loop
				if (loopCount > 100) break;
			}
			while (matchFound);
			StaticHelpers.Logger.Verbose($"Stage 4.1: {sb}");
		}

		private void ResolveProxyMod(StringBuilder sb, ICollection<Entry> entriesWithProxies, TranslationResults result,
			Dictionary<string, ProxiesWithCount> proxies, Entry proxyMod, ref bool matchFound)
		{
			var input = Stage4P1InputRegex.Replace(proxyMod.Input, @"\[\[$1#(\d+)]]");
			var matches = Regex.Matches(sb.ToString(), input).Cast<Match>().ToList();
			if (matches.Count == 0) return;
			var groupedMatches = matches.GroupBy(m => m.Value);
			matchFound = true;
			foreach (var match in groupedMatches)
			{
				ResolveMatchGroup(match, sb, entriesWithProxies, proxies, proxyMod, result);
			}
		}

		private void ResolveMatchGroup(IGrouping<string, Match> matchGroup,
			StringBuilder sb, ICollection<Entry> entriesWithProxies,
			Dictionary<string, ProxiesWithCount> proxies, Entry proxyMod,
			TranslationResults result)
		{
			var proxyMatches = matchGroup.SelectMany(x => x.Groups.Cast<Group>().Skip(1)
				.Select(g => int.Parse(g.Value))).Distinct().ToList();
			var merge = proxyMatches.Count > 1;
			var newEntry = new Entry
			{
				RoleString = proxyMod.RoleString,
				Input = matchGroup.Key,
				Output = proxyMod.Output
			};
			for (var index = 0; index < proxyMatches.Count; index++)
			{
				int proxyId = proxyMatches[index];
				var matchedEntry = entriesWithProxies.Single(x =>
				{
					var mainProxyPart = x.AssignedProxy.MainRole;
					return x.AssignedProxy.Id == proxyId && mainProxyPart == proxyMod.RoleString;
				});
				if (merge)
				{
					if (newEntry.AssignedProxy == null) AssignProxy(proxies, newEntry);
					Debug.Assert(newEntry.AssignedProxy != null, "mergedEntry.AssignedProxy != null");
					newEntry.AssignedProxy.ProxyMods.AddRange(matchedEntry.AssignedProxy.ProxyMods);
					newEntry.Output = newEntry.Output.Replace(matchedEntry.AssignedProxy.WithId(index + 1), matchedEntry.Output);
				}
				else
				{
					newEntry = new Entry
					{
						RoleString = matchedEntry.RoleString,
						Input = matchedEntry.Input,
						Output = matchedEntry.Output
					};
					AssignProxy(proxies, newEntry);
					newEntry.AssignedProxy.ProxyMods.Add(proxyMod);
					entriesWithProxies.Add(newEntry);
					var output = newEntry.AssignedProxy.FullRoleString;
					var thisInput = Stage4P1InputRegex.Replace(proxyMod.Input, Regex.Escape(matchedEntry.AssignedProxy.FullRoleString));
					LogReplaceRegex(sb, thisInput, output, result, proxyMod);
				}
			}

			if (merge && proxyMatches.Count > 1)
			{
				entriesWithProxies.Add(newEntry);
				var output = newEntry.AssignedProxy.FullRoleString;
				LogReplaceRegex(sb, Regex.Escape(matchGroup.Key), output, result, proxyMod);
			}
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
		public int Used { get; set; }
		public int Count { get; set; }

		public ProxiesWithCount(IEnumerable<RoleProxy> proxies)
		{
			Proxies = new Queue<RoleProxy>(proxies);
			Count = 0;
		}
	}
}
