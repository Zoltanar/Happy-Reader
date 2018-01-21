using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;
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

        public override bool Equals(object obj)
        {
            if(obj is VNItem otherVN) return ID == otherVN.ID;
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        public bool Equals(VNItem other)
        {
            // ReSharper disable once PossibleNullReferenceException
            return ID == other.ID;
        }

        public override string ToString() => $"ID={ID} Title={Title}";
        
        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            var hashID = ID == -1 ? 0 : ID.GetHashCode();
            // ReSharper restore NonReadonlyMemberInGetHashCode
            return hashID;
        }
        /// <summary>
        /// Saves a VN's cover image (unless it already exists)
        /// </summary>
        /// <param name="update">Get new image regardless of whether one already exists?</param>
        public void SaveCover(bool update = false)
        {
            if (!Directory.Exists(VNImagesFolder)) Directory.CreateDirectory(VNImagesFolder);
            if (Image == null || Image.Equals("")) return;
            string imageLocation = $"{VNImagesFolder}{ID}{Path.GetExtension(Image)}";
            if (File.Exists(imageLocation) && update == false) return;
            LogToFile($"Start downloading cover image for {this}");
            try
            {
                var requestPic = WebRequest.Create(Image);
                using (var responsePic = requestPic.GetResponse())
                {
                    using (var stream = responsePic.GetResponseStream())
                    {
                        if (stream == null) return;
                        var webImage = System.Drawing.Image.FromStream(stream);
                        webImage.Save(imageLocation);
                    }
                }
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is ArgumentNullException || ex is SecurityException || ex is UriFormatException || ex is ExternalException)
            { LogToFile(ex); }
        }

        /// <summary>
        /// Saves a title's cover image (unless it already exists)
        /// </summary>
        public async Task SaveCoverAsync(bool update = false)
        {
            if (!Directory.Exists(VNImagesFolder)) Directory.CreateDirectory(VNImagesFolder);
            if (Image == null || Image.Equals("")) return;
            string imageLocation = $"{VNImagesFolder}{ID}{Path.GetExtension(Image)}";
            if (File.Exists(imageLocation) && update == false) return;
            LogToFile($"Start downloading cover image for {this}");
            try
            {
                var requestPic = WebRequest.Create(Image);
                using (var responsePic = await requestPic.GetResponseAsync())
                {
                    using (var stream = responsePic.GetResponseStream())
                    {
                        if (stream == null) return;
                        var webImage = System.Drawing.Image.FromStream(stream);
                        webImage.Save(imageLocation);
                    }
                }
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is ArgumentNullException || ex is SecurityException || ex is UriFormatException || ex is ExternalException)
            { LogToFile(ex); }
        }

        #region Subclasses

        /// <summary>
        /// In VNItem, when details flag is present
        /// </summary>
        public class AnimeItem
        {
            public int ID { get; set; }
            public int? Ann_ID { get; set; }
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

            /// <summary>
            /// Get path of stored screenshot (Doesn't check if it exists).
            /// </summary>
            public string StoredLocation
            {
                get
                {
                    string[] urlSplit = Image.Split('/');
                    return $"{VNScreensFolder}{urlSplit[urlSplit.Length - 2]}\\{urlSplit[urlSplit.Length - 1]}";
                }
            }

            /// <summary>
            /// Downloads screenshot into screens folder
            /// </summary>
            public void Download()
            {
                if (string.IsNullOrWhiteSpace(Image)) return;
                if (!Directory.Exists(VNScreensFolder)) Directory.CreateDirectory(VNScreensFolder);
                string[] urlSplit = Image.Split('/');
                if (!Directory.Exists($"{VNScreensFolder}\\{urlSplit[urlSplit.Length - 2]}"))
                    Directory.CreateDirectory($"{VNScreensFolder}\\{urlSplit[urlSplit.Length - 2]}");
                if (File.Exists(StoredLocation)) return;
                var requestPic = WebRequest.Create(Image);
                using (var responsePic = requestPic.GetResponse())
                {
                    using (var stream = responsePic.GetResponseStream())
                    {
                        if (stream == null) return;
                        var webImage = System.Drawing.Image.FromStream(stream);
                        webImage.Save(StoredLocation);
                    }
                }
            }
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
                {"seq", "Sequel"},
                {"preq", "Prequel"},
                {"set", "Same Setting"},
                {"alt", "Alternative Version"},
                {"char", "Shares Characters"},
                {"side", "Side Story"},
                {"par", "Parent Story"},
                {"ser", "Same Series"},
                {"fan", "Fandisc"},
                {"orig", "Original Game"}
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

            public override string ToString() => $"[{ID},{Score},{Spoiler}]";


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

        #endregion

    }
}