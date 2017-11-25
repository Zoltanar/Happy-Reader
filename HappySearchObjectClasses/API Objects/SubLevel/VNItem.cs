using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

namespace Happy_Apps_Core
{
    /// <summary>
    /// From get vn commands
    /// </summary>
    public class VNItem : IEquatable<VNItem>
    {
        public int ID { get; set; }
        //flag: basic
        public string Title { get; set; }
        /// <summary>
        /// Original title (Kanji title)
        /// </summary>
        public string Original { get; set; }
        public string Released { get; set; }
        public string[] Languages { get; set; }
        public string[] Orig_Lang { get; set; }
        public List<string> Platforms { get; set; }
        //flag: details
        public string Aliases { get; set; }
        public int? Length { get; set; }
        public string Description { get; set; }
        public WebLinks Links { get; set; }
        public string Image { get; set; }
        public bool Image_Nsfw { get; set; }
        public AnimeItem[] Anime { get; set; } //flag: anime
        public RelationsItem[] Relations { get; set; } //flag: relations
        public List<TagItem> Tags { get; set; } //flag: tags
        //flag: stats
        public double Popularity { get; set; }
        public double Rating { get; set; }
        public int VoteCount { get; set; }
        public ScreenItem[] Screens { get; set; } //flag: screens

        public bool Equals(VNItem other)
        {
            // ReSharper disable once PossibleNullReferenceException
            return ID == other.ID;
        }


        public override string ToString() => $"ID={ID} Title={Title}";

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var hashID = ID == -1 ? 0 : ID.GetHashCode();
            return hashID;
        }

        /// <summary>
        /// In VNItem, when details flag is present
        /// </summary>
        public class AnimeItem
        {
            public int ID { get; set; }
            public int Ann_ID { get; set; }
            public string Nfo_ID { get; set; }
            public string Title_Romaji { get; set; }
            public string Title_Kanji { get; set; }
            public int Year { get; set; }
            public string Type { get; set; }

            public string Print()
            {
                var sb = new StringBuilder();
                if (Title_Romaji != null) sb.Append(Title_Romaji);
                else if (Title_Kanji != null) sb.Append(Title_Kanji);
                else sb.Append(ID);
                if (Year > 0) sb.Append($" ({Year})");
                if (Type != null) sb.Append($" ({Type})");
                return sb.ToString();
            }
        }

        /// <summary>
        /// In VNItem, when details screens is present
        /// </summary>
        public class ScreenItem
        {
            /// <summary>
            /// URL of screenshot 
            /// </summary>
            public string Image { get; set; }
            public int RID { get; set; }
            public bool Nsfw { get; set; }
            public int Height { get; set; }
            public int Width { get; set; }
        }

        /// <summary>
        /// In VNItem, when relations flag is present
        /// </summary>
        public class RelationsItem
        {
            public int ID { get; set; }
            public string Relation { get; set; }
            public string Title { get; set; }
            public string Original { get; set; }
            public bool Official { get; set; }

            public static readonly Dictionary<string, string> relationDict = new Dictionary<string, string>
            {
                { "seq", "Sequel"},
                { "preq", "Prequel"},
                { "set", "Same Setting"},
                { "alt", "Alternative Version"},
                { "char", "Shares Characters"},
                { "side", "Side Story"},
                { "par", "Parent Story"},
                { "ser", "Same Series"},
                { "fan", "Fandisc"},
                { "orig", "Original Game"}
            };

            public string Print() => $"{(Official ? "" : "[Unofficial] ")}{relationDict[Relation]} - {Title} - {ID}";

            public override string ToString() => $"ID={ID} Title={Title}";
        }

        /// <summary>
        /// In VNItem, when details flag is present
        /// </summary>
        public class WebLinks
        {
            public object Renai { get; set; }
            public object Encubed { get; set; }
            public object Wikipedia { get; set; }
        }

        /// <summary>
        /// In VNItem, when tags flag is present
        /// </summary>
        public class TagItem : List<double>
        {
            public int ID
            {
                get => (int)this[0];
                set => this[0] = value;
            }

            public double Score
            {
                get => this[1];
                set => this[1] = value;
            }

            public int Spoiler
            {
                get => (int)this[2];
                set => this[2] = value;
            }

            public StaticHelpers.TagCategory Category { get; set; }

            public override string ToString()
            {
                return $"[{ID},{Score},{Spoiler}]";
            }

            public string GetName(List<DumpFiles.WrittenTag> plainTags)
            {
                return plainTags.Find(item => item.ID == ID)?.Name;
            }

            public void SetCategory(List<DumpFiles.WrittenTag> plainTags)
            {
                string cat = plainTags.Find(item => item.ID == ID)?.Cat;
                switch (cat)
                {
                    case StaticHelpers.ContentTag:
                        Category = StaticHelpers.TagCategory.Content;
                        return;
                    case StaticHelpers.SexualTag:
                        Category = StaticHelpers.TagCategory.Sexual;
                        return;
                    case StaticHelpers.TechnicalTag:
                        Category = StaticHelpers.TagCategory.Technical;
                        return;
                    default:
                        return;
                }
            }

            /// <summary>
            /// Return string with Tag name and score, if tag isn't found in list, "Not Approved" is returned.
            /// </summary>
            /// <param name="plainTags">List of tags from tagdump</param>
            /// <returns>String with tag name and score</returns>
            public string Print(List<DumpFiles.WrittenTag> plainTags)
            {
                var name = GetName(plainTags);
                return name != null ? $"{GetName(plainTags)} ({Score:0.00})" : "Not Approved";
            }
        }


        /// <summary>
        /// Custom Json class for having notes and groups as a string in title notes.
        /// </summary>
        public class CustomItemNotes
        {
            public string Notes { get; set; }
            public List<string> Groups { get; set; }

            public CustomItemNotes(string notes, List<string> groups)
            {
                Notes = notes;
                Groups = groups;
            }

            public string Serialize()
            {
                if (Notes.Equals("") && !Groups.Any()) return "";
                string serializedString = $"Notes: {Notes}|Groups: {string.Join(",", Groups)}";
                string escapedString = JsonConvert.ToString(serializedString);
                return escapedString.Substring(1, escapedString.Length - 2);
            }
        }


    }
}