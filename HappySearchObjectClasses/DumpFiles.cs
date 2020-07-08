using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core
{
	public static partial class DumpFiles
	{
		private static readonly string TagsJsonGz = Path.Combine(StoredDataFolder, "tags.json.gz");
		private static readonly string TraitsJsonGz = Path.Combine(StoredDataFolder, "traits.json.gz");
		private static readonly string TagsJson = Path.Combine(StoredDataFolder, "tags.json");
		private static readonly string TraitsJson = Path.Combine(StoredDataFolder, "traits.json");

		public static bool Loaded { get; private set; }

		public const string ContentTag = "cont";
		public const string SexualTag = "ero";
		public const string TechnicalTag = "tech";

		// ReSharper disable UnusedMember.Global

		/// <summary>
		/// Object contained in tag dump file
		/// </summary>
		// ReSharper disable ClassNeverInstantiated.Global
		public class WrittenTag : ItemWithParents
		{
			public int VNs { get; set; }
			public string Cat { get; set; }

			public override bool InCollection(IEnumerable<int> idCollection, out int match)
			{
				//match = idCollection.FirstOrDefault(id => GetTag(id)?.Parents.Contains(ID) ?? false);
				match = idCollection.FirstOrDefault(id => AllIDs.Contains(id));
				return match != default;
			}

			/// <summary>Returns a string that represents the current object.</summary>
			/// <returns>A string that represents the current object.</returns>
			/// <filterpriority>2</filterpriority>
			public override string ToString() => Name;

		}

		/// <summary>
		/// Object contained in trait dump file
		/// </summary>
		public class WrittenTrait : ItemWithParents
		{
			public int Chars { get; set; }

			public int TopmostParent { get; set; }
			public string TopmostParentName { get; set; }

			public void SetTopmostParent(List<WrittenTrait> plainTraits)
			{
				if (Parents.Count == 0)
				{
					TopmostParent = ID;
					return;
				}
				var idOfParent = Parents.First();
				while (plainTraits.Find(x => x.ID == idOfParent).Parents.Count > 0)
				{
					List<int> parents = plainTraits.Find(x => x.ID == idOfParent).Parents;
					idOfParent = parents.First();
				}
				TopmostParent = idOfParent;
				TopmostParentName = plainTraits.Find(x => x.ID == TopmostParent).Name;
			}

			public override bool InCollection(IEnumerable<int> idCollection, out int match)
			{
				match = idCollection.FirstOrDefault(id => GetTrait(id)?.Parents.Contains(ID) ?? false);
				return match != default;
			}


			public bool InCollection(IEnumerable<DbTrait> traitCollection, out DbTrait match)
			{
				match = traitCollection.FirstOrDefault(trait=> GetTrait(trait.TraitId)?.Parents.Contains(ID) ?? false);
				return match != default;
			}

			/// <summary>Returns name of tag in the form of Root > Trait (e.g. Hair > Green)</summary>
			public override string ToString() => $"{TopmostParentName} > {Name}";
		}

		// ReSharper restore ClassNeverInstantiated.Global
		// ReSharper restore UnusedMember.Global

		/// <summary>
		/// Contains all tags as in tags.json, key is tag id
		/// </summary>
		private static Dictionary<int, WrittenTag> _plainTags;

		/// <summary>
		/// Contains all traits as in traits.json, key is trait id
		/// </summary>
		private static Dictionary<int, WrittenTrait> _plainTraits;

		private const int MaxTries = 5;

		private static bool GetNewDumpFiles()
		{
			bool b1 = GetTagDump();
			//trait dump section
			bool b2 = GetTraitDump();
			return b1 || b2;
		}

		/// <summary>
		/// Return true if a new file was downloaded
		/// </summary>
		private static bool GetTagDump()
		{
			int tries = 0;
			bool complete = false;
			//tag dump section
			while (!complete && tries < MaxTries)
			{
				if (File.Exists(TagsJsonGz)) continue;
				tries++;
				try
				{
					using (var client = new WebClient())
					{
						client.DownloadFile(TagsURL, TagsJsonGz);
					}
					GZipDecompress(TagsJsonGz, TagsJson);
					File.Delete(TagsJsonGz);
					complete = true;
				}
				catch (Exception e)
				{
					Logger.ToFile(e);
				}
			}
			//load default file if new one couldn't be received or for some reason doesn't exist.
			if (!complete || !File.Exists(TagsJson))
			{
				LoadTagDump(true);
				return false;
			}
			LoadTagDump();
			return true;
		}

		/// <summary>
		/// Return true if a new file was downloaded
		/// </summary>
		private static bool GetTraitDump()
		{
			int tries = 0;
			bool complete = false;
			while (!complete && tries < MaxTries)
			{
				if (File.Exists(TraitsJsonGz)) continue;
				tries++;
				try
				{
					using (var client = new WebClient())
					{
						client.DownloadFile(TraitsURL, TraitsJsonGz);
					}
					GZipDecompress(TraitsJsonGz, TraitsJson);
					File.Delete(TraitsJsonGz);
					complete = true;
				}
				catch (Exception e)
				{
					Logger.ToFile(e);
				}
			}
			//load default file if new one couldn't be received or for some reason doesn't exist.
			if (!complete || !File.Exists(TraitsJson))
			{
				LoadTraitDump(true);
				return false;
			}
			LoadTraitDump();
			return true;
		}

		/// <summary>
		///     Load Tags from Tag dump file.
		/// </summary>
		/// <param name="loadDefault">Load default file?</param>
		private static void LoadTagDump(bool loadDefault = false)
		{
			if (!loadDefault) loadDefault = !File.Exists(TagsJson);
			var fileToLoad = loadDefault ? DefaultTagsJson : TagsJson;
			Logger.ToFile($"Attempting to load {fileToLoad}");
			_plainTags = new Dictionary<int, WrittenTag>();
			try
			{
				var plainTags = JsonConvert.DeserializeObject<List<WrittenTag>>(File.ReadAllText(fileToLoad));
				List<ItemWithParents> baseList = plainTags.Cast<ItemWithParents>().ToList();
				foreach (var writtenTag in plainTags)
				{
					writtenTag.SetItemChildren(baseList);
					_plainTags.Add(writtenTag.ID, writtenTag);
				}
			}
			catch (JsonReaderException e)
			{
				if (fileToLoad.Equals(DefaultTagsJson))
				{
					//Should never happen.
					Logger.ToFile($"Failed to read default tags.json file, please download a new one from {TagsURL} uncompress it and paste it in {DefaultTagsJson}.");
					_plainTags.Clear();
					return;
				}
				Logger.ToFile(e);
				File.Delete(TagsJson);
				LoadTagDump(true);
			}
		}

		/// <summary>
		///     Load Traits from Trait dump file.
		/// </summary>
		private static void LoadTraitDump(bool loadDefault = false)
		{
			if (!loadDefault) loadDefault = !File.Exists(TraitsJson);
			var fileToLoad = loadDefault ? DefaultTraitsJson : TraitsJson;
			Logger.ToFile($"Attempting to load {fileToLoad}");
			_plainTraits = new Dictionary<int, WrittenTrait>();
			try
			{
				var plainTraits = JsonConvert.DeserializeObject<List<WrittenTrait>>(File.ReadAllText(fileToLoad));
				List<ItemWithParents> baseList = plainTraits.Cast<ItemWithParents>().ToList();
				foreach (var writtenTrait in plainTraits)
				{
					writtenTrait.SetTopmostParent(plainTraits);
					writtenTrait.SetItemChildren(baseList);
					_plainTraits.Add(writtenTrait.ID, writtenTrait);

				}
			}
			catch (JsonReaderException e)
			{
				if (fileToLoad.Equals(DefaultTraitsJson))
				{
					//Should never happen.
					Logger.ToFile($"Failed to read default traits.json file, please download a new one from {TraitsURL} uncompress it and paste it in {DefaultTraitsJson}.");
					_plainTraits.Clear();
					return;
				}
				Logger.ToFile(e);
				File.Delete(TraitsJson);
				LoadTraitDump(true);
			}
		}

		public static void Load(bool reDownload = true)
		{
			var daysSince = CSettings.DumpfileDate.DaysSince();
			try
			{
				if (reDownload && (daysSince > 2 || daysSince == -1))
				{
					if (!GetNewDumpFiles()) return;
					CSettings.DumpfileDate = DateTime.UtcNow;
				}
				else
				{
					//load dump files if they exist, otherwise load default.
					LoadTagDump();
					LoadTraitDump();
				}
			}
			finally
			{
				Loaded = true;
			}
		}

		/// <summary>
		/// List of possible root traits, their int is their ID
		/// </summary>
		public enum RootTrait
		{
			None = 0,
			Hair = 1,
			Eyes = 35,
			Body = 36,
			Clothes = 37,
			Items = 38,
			Personality = 39,
			Role = 40,
			EngagesIn = 41,
			SubjectOf = 42,
			EngagesInSexual = 43,
			SubjectOfSexual = 1625
		}

		public static WrittenTrait GetTrait(RootTrait root, string traitName) => _plainTraits.Values.FirstOrDefault(x => x.Name.Equals(traitName) && x.TopmostParent == (int)root);

		public static WrittenTrait GetTrait(int traitId) => _plainTraits.TryGetValue(traitId, out var trait) ? trait : null;

		public static WrittenTag GetTag(string name) => _plainTags.Values.FirstOrDefault(x => x.Name == name);

		public static WrittenTag GetTag(int tagId) => _plainTags.TryGetValue(tagId, out var tag) ? tag : null;
	}
}
