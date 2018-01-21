using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core
{
    public static class DumpFiles
    {
        private const string TagsJsonGz = StoredDataFolder + "tags.json.gz";
        private const string TraitsJsonGz = StoredDataFolder + "traits.json.gz";
        private const string TagsJson = StoredDataFolder + "tags.json";
        private const string TraitsJson = StoredDataFolder + "traits.json";
        
        public const string ContentTag = "cont";
        public const string SexualTag = "ero";
        public const string TechnicalTag = "tech";

        // ReSharper disable UnusedMember.Global
        public class ItemWithParents
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Aliases { get; set; }
            public bool Meta { get; set; }
            public List<int> Parents { get; set; }
            public int[] Children { get; set; }
            public int[] AllIDs { get; set; }

            public void SetItemChildren(List<ItemWithParents> list)
            {

                int[] children = Enumerable.Empty<int>().ToArray();
                //LogToFile($"Getting children for {this}");
                //new
                int[] childrenForThisRound = list.Where(x => x.Parents.Contains(ID)).Select(x => x.ID).ToArray(); //at this moment, it contains direct subtags
                var difference = childrenForThisRound.Length;
                while (difference > 0)
                {
                    var initial = children.Length;
                    //debug printout
                    //IEnumerable<ItemWithParents> debuglist = childrenForThisRound.Select(childID => list.Find(x => x.ID == childID));
                    //Debug.WriteLine(string.Join(", ", debuglist));
                    //
                    children = children.Union(childrenForThisRound).ToArray(); //first time, adds direct subtags, second time it adds 2-away subtags, etc...
                    difference = children.Length - initial;
                    var tmp = new List<int>();
                    foreach (var child in childrenForThisRound)
                    {
                        IEnumerable<int> childsChildren = list.Where(x => x.Parents.Contains(child)).Select(x => x.ID);
                        //LogToFile($"{child} has {childsChildren.Count()}");
                        tmp.AddRange(childsChildren);
                    }
                    childrenForThisRound = tmp.ToArray();
                }
                Children = children;
                AllIDs = children.Union(new[] { ID }).ToArray();
            }
        }

        /// <summary>
        /// Object contained in tag dump file
        /// </summary>
        // ReSharper disable ClassNeverInstantiated.Global
        public class WrittenTag : ItemWithParents
        {
            public int VNs { get; set; }
            public string Cat { get; set; }

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

            /// <summary>Returns name of tag in the form of Root > Trait (e.g. Hair > Green)</summary>
            public override string ToString() => $"{TopmostParentName} > {Name}";

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
        }
        // ReSharper restore ClassNeverInstantiated.Global
        // ReSharper restore UnusedMember.Global

        /// <summary>
        /// Contains all tags as in tags.json
        /// </summary>
        public static List<WrittenTag> PlainTags { get; private set; }

        /// <summary>
        /// Contains all traits as in traits.json
        /// </summary>
        public static List<WrittenTrait> PlainTraits { get; private set; }

        private const int MaxTries = 5;

        private static bool GetNewDumpFiles()
        {
            bool b1 = GetTagdump();
            //traitdump section
            bool b2 = GetTraitdump();
            return b1 || b2;
        }

        /// <summary>
        /// Return true if a new file was downloaded
        /// </summary>
        private static bool GetTagdump()
        {
            int tries = 0;
            bool complete = false;
            //tagdump section
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
                    LogToFile(e);
                }
            }
            //load default file if new one couldnt be received or for some reason doesn't exist.
            if (!complete || !File.Exists(TagsJson))
            {
                LoadTagdump(true);
                return false;
            }
            LoadTagdump();
            return true;
        }

        /// <summary>
        /// Return true if a new file was downloaded
        /// </summary>
        private static bool GetTraitdump()
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
                    LogToFile(e);
                }
            }
            //load default file if new one couldnt be received or for some reason doesn't exist.
            if (!complete || !File.Exists(TraitsJson))
            {
                LoadTraitdump(true);
                return false;
            }
            LoadTraitdump();
            return true;
        }

        /// <summary>
        ///     Load Tags from Tag dump file.
        /// </summary>
        /// <param name="loadDefault">Load default file?</param>
        private static void LoadTagdump(bool loadDefault = false)
        {
            if (!loadDefault) loadDefault = !File.Exists(TagsJson);
            var fileToLoad = loadDefault ? DefaultTagsJson : TagsJson;
            LogToFile($"Attempting to load {fileToLoad}");
            try
            {
                PlainTags = JsonConvert.DeserializeObject<List<WrittenTag>>(File.ReadAllText(fileToLoad));
                List<ItemWithParents> baseList = PlainTags.Cast<ItemWithParents>().ToList();
                foreach (var writtenTag in PlainTags)
                {
                    writtenTag.SetItemChildren(baseList);
                }
            }
            catch (JsonReaderException e)
            {
                if (fileToLoad.Equals(DefaultTagsJson))
                {
                    //Should never happen.
                    LogToFile($"Failed to read default tags.json file, please download a new one from {TagsURL} uncompress it and paste it in {DefaultTagsJson}.");
                    PlainTags = new List<WrittenTag>();
                    return;
                }
                LogToFile(e);
                File.Delete(TagsJson);
                LoadTagdump(true);
            }
        }

        /// <summary>
        ///     Load Traits from Trait dump file.
        /// </summary>
        private static void LoadTraitdump(bool loadDefault = false)
        {
            if (!loadDefault) loadDefault = !File.Exists(TraitsJson);
            var fileToLoad = loadDefault ? DefaultTraitsJson : TraitsJson;
            LogToFile($"Attempting to load {fileToLoad}");
            try
            {
                PlainTraits = JsonConvert.DeserializeObject<List<WrittenTrait>>(File.ReadAllText(fileToLoad));
                List<ItemWithParents> baseList = PlainTraits.Cast<ItemWithParents>().ToList();
                foreach (var writtenTrait in PlainTraits)
                {
                    writtenTrait.SetTopmostParent(PlainTraits);
                    writtenTrait.SetItemChildren(baseList);
                }
            }
            catch (JsonReaderException e)
            {
                if (fileToLoad.Equals(DefaultTraitsJson))
                {
                    //Should never happen.
                    LogToFile($"Failed to read default traits.json file, please download a new one from {TraitsURL} uncompress it and paste it in {DefaultTraitsJson}.");
                    PlainTraits = new List<WrittenTrait>();
                    return;
                }
                LogToFile(e);
                File.Delete(TraitsJson);
                LoadTraitdump(true);
            }
        }

        public static void Load()
        {
            var daysSince = CSettings.DumpfileDate.DaysSince();
            if (daysSince > 2 || daysSince == -1)
            {
                if (!GetNewDumpFiles()) return;
                CSettings.DumpfileDate = DateTime.UtcNow;
            }
            else
            {
                //load dump files if they exist, otherwise load default.
                LoadTagdump();
                LoadTraitdump();
            }
        }
        
    }
}
