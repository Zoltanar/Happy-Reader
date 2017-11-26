using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Timer = System.Windows.Forms.Timer;

namespace Happy_Apps_Core
{
    /// <summary>
    /// Static Methods
    /// </summary>
    public static class StaticHelpers
    {
        #region File Locations

#pragma warning disable 1591
        public const string TagsURL = "http://vndb.org/api/tags.json.gz";
        public const string TraitsURL = "http://vndb.org/api/traits.json.gz";
        public const string ProjectURL = "https://github.com/Zoltanar/Happy-Search";
        public const string DefaultTraitsJson = "Program Data\\Default Files\\traits.json";
        public const string DefaultTagsJson = "Program Data\\Default Files\\tags.json";
        public const string DefaultFiltersJson = "Program Data\\Default Files\\defaultfilters.json";
        public const string FlagsFolder = "Program Data\\Flags\\";

#if HAPPYREADER
        public const string StoredDataFolder = @"C:\Users\Gusty\Documents\VNPC-By Zoltanar\Visual Novel Database\Visual Novel Database\bin\x64\Release\Stored Data\";
#elif DEBUG
        public const string StoredDataFolder = "..\\Release\\Stored Data\\";
#else
        public const string StoredDataFolder = "Stored Data\\";
#endif

        public const string VNImagesFolder = StoredDataFolder + "Saved Cover Images\\";
        public const string VNScreensFolder = StoredDataFolder + "Saved Screenshots\\";
        public const string DBStatsJson = StoredDataFolder + "dbs.json";
        public const string SettingsJson = StoredDataFolder + "settings.json";
        public const string CustomFiltersJson = StoredDataFolder + "customfilters.json";
        public const string PermanentFilterJson = StoredDataFolder + "filters.json";
        public const string LogFile = StoredDataFolder + "message.log";
        public const string CoreSettingsJson = StoredDataFolder + "coresettings.json";
#pragma warning restore 1591

        #endregion

#pragma warning disable 1591
        public const string ClientName = "Happy Search";
        public const string ClientVersion = "1.5.0";
        public const string APIVersion = "2.25";
        public const int APIMaxResults = 25;
        public static readonly string MaxResultsString = "\"results\":" + APIMaxResults;
        public const string TagTypeUrt = "mctULLabel";
        public const string ContentTag = "cont";
        public const string SexualTag = "ero";
        public const string TechnicalTag = "tech";
        private const int LabelFadeTime = 5000;

        //tile background colors
        public static readonly SolidBrush DefaultTileBrush = new SolidBrush(Color.LightBlue);
        public static readonly SolidBrush WLHighBrush = new SolidBrush(Color.DeepPink);
        public static readonly SolidBrush WLMediumBrush = new SolidBrush(Color.HotPink);
        public static readonly SolidBrush WLLowBrush = new SolidBrush(Color.LightPink);
        public static readonly SolidBrush ULFinishedBrush = new SolidBrush(Color.LightGreen);
        public static readonly SolidBrush ULStalledBrush = new SolidBrush(Color.DarkKhaki);
        public static readonly SolidBrush ULDroppedBrush = new SolidBrush(Color.DarkOrange);
        public static readonly SolidBrush ULUnknownBrush = new SolidBrush(Color.Gray);

        //tile text colors
        public static readonly SolidBrush FavoriteProducerBrush = new SolidBrush(Color.Yellow);
        public static readonly SolidBrush ULPlayingBrush = new SolidBrush(Color.Yellow);
        public static readonly SolidBrush UnreleasedBrush = new SolidBrush(Color.White);

        //text colors
        private static readonly Color ErrorColor = Color.Red;
        private static readonly Color NormalColor = SystemColors.ControlLightLight;
        private static readonly Color NormalLinkColor = Color.FromArgb(0, 192, 192);
        private static readonly Color WarningColor = Color.DarkKhaki;
        
        public static bool DontTriggerEvent; //used to skip indexchanged events

        /// <summary>
        /// Categories of VN Tags
        /// </summary>
        public enum TagCategory { Content, Sexual, Technical }

        public static readonly CoreSettings Settings = CoreSettings.Load();

        public static VndbConnection Conn;
#pragma warning restore 1591

        static StaticHelpers()
        {
            Directory.CreateDirectory(StoredDataFolder);
            File.Create(LogFile).Close();
        }

        #region ADO Database
        
        public static VNDatabase LocalDatabase;

        public static bool VNIsByFavoriteProducer(ListedVN vn)
        {
            return LocalDatabase.FavoriteProducerList.Any(x => x.ID == vn.ProducerID);
        }
        
#endregion
        /// <summary>
        /// Get brush from vn UL or WL status or null if no statuses are found.
        /// </summary>
        public static SolidBrush GetBrushFromStatuses(ListedVN vnBase)
        {
            if (vnBase == null) return null;
            var brush = GetColorFromULStatus(vnBase.UserVN.ULStatus);
            if (brush != null) return brush;
            brush = GetColorFromWLStatus(vnBase.UserVN.WLStatus);
            return brush;
        }

        /// <summary>
        /// Return color based on wishlist status, or null if no status
        /// </summary>
        public static SolidBrush GetColorFromWLStatus(WishlistStatus? status)
        {
            if (status == null) return null;
            switch (status)
            {
                case WishlistStatus.High:
                    return WLHighBrush;
                case WishlistStatus.Medium:
                    return WLMediumBrush;
                case WishlistStatus.Low:
                    return WLLowBrush;
            }
            return null;
        }

        /// <summary>
        /// Return color based on userlist status, or null if no status
        /// </summary>
        public static SolidBrush GetColorFromULStatus(UserlistStatus? status)
        {
            if (status == null) return null;
            switch (status)
            {
                case UserlistStatus.Finished:
                    return ULFinishedBrush;
                case UserlistStatus.Stalled:
                    return ULStalledBrush;
                case UserlistStatus.Dropped:
                    return ULDroppedBrush;
                case UserlistStatus.Unknown:
                    return ULUnknownBrush;
            }
            return null;
        }

        /// <summary>
        /// Serialize object to JSON string and save to file.
        /// </summary>
        public static void SaveObjectToJsonFile<T>(T objectToSave, string filePath)
        {
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(objectToSave, Formatting.Indented));
            }
            catch (Exception e)
            {
                LogToFile("Couldn't save object to file", e);
            }
        }

        /// <summary>
        /// Deserialize object from JSON string in file.
        /// </summary>
        public static T LoadObjectFromJsonFile<T>(string filePath)
        {
            T returned;
            try
            {
                returned = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
            }
            catch (Exception e)
            {
                LogToFile("Couldn't save object to file", e);
                returned = default(T);
            }
            return returned;
        }

        /// <summary>
        /// Pause RaiseListChangedEvents and add items then call the event when done adding.
        /// </summary>
        public static void AddRange<T>(this BindingList<T> list, IEnumerable<T> items)
        {
            if (items == null) return;
            list.RaiseListChangedEvents = false;
            foreach (var item in items) list.Add(item);
            list.RaiseListChangedEvents = true;
            list.ResetBindings();
        }

        /// <summary>
        /// Pause RaiseListChangedEvents, clear list and add items, then call ResetBindings event.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public static void SetRange<T>(this BindingList<T> list, IEnumerable<T> items)
        {
            list.RaiseListChangedEvents = false;
            list.Clear();
            foreach (var item in items) list.Add(item);
            list.RaiseListChangedEvents = true;
            list.ResetBindings();
        }

        /// <summary>
        /// Print message to Debug and write it to log file.
        /// </summary>
        /// <param name="message">Message to be written</param>
        public static void LogToFile(string message)
        {
            Debug.Print(message);
	        int counter = 0;
	        while (IsFileLocked(new FileInfo(LogFile)))
	        {
		        counter++;
		        if (counter > 5) throw new IOException("Logfile is locked!");
		        Thread.Sleep(25);
	        }
			using (var writer = new StreamWriter(LogFile, true))
            {
                writer.WriteLine(message);
            }
        }

        /// <summary>
        /// Print exception to Debug and write it to log file.
        /// </summary>
        /// <param name="header">Human-given location or reason for error</param>
        /// <param name="exception">Exception to be written to file</param>
        public static void LogToFile(string header, Exception exception)
        {
            Debug.Print(header);
            Debug.Print(exception.Message);
            Debug.Print(exception.StackTrace);
						int counter = 0;
						while (IsFileLocked(new FileInfo(LogFile)))
						{
						  counter++;
						  if (counter > 5) throw new IOException("Logfile is locked!");
						  Thread.Sleep(25);
						}
						using (var writer = new StreamWriter(LogFile, true))
            {
                writer.WriteLine(header);
                writer.WriteLine(exception.Message);
                writer.WriteLine(exception.StackTrace);
            }
        }

        /// <summary>
        /// Check if file is locked,
        /// </summary>
        /// <param name="file">File to be checked</param>
        /// <returns>Whether file is locked</returns>
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }
            return false;
        }

        /// <summary>
        /// Gets description of Enum value if available.
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());

            return !(Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute) ? value.ToString() : attribute.Description;
        }

        /// <summary>
        ///     Writes message in a label with message text color.
        /// </summary>
        /// <param name="label">Label to which the message is written</param>
        /// <param name="message">Message to be written</param>
        public static void WriteText(Label label, string message)
        {
            if (label is LinkLabel linkLabel) linkLabel.LinkColor = NormalLinkColor;
            else label.ForeColor = NormalColor;
            label.Text = message;
        }

        /// <summary>
        ///     Writes message in a label with warning text color.
        /// </summary>
        /// <param name="label">Label to which the message is written</param>
        /// <param name="message">Message to be written</param>
        public static void WriteWarning(Label label, string message)
        {
            if (label is LinkLabel linkLabel) linkLabel.LinkColor = WarningColor;
            else label.ForeColor = WarningColor;
            label.Text = message;
        }

        /// <summary>
        ///     Writes message in a label with error text color.
        /// </summary>
        /// <param name="label">Label to which the message is written</param>
        /// <param name="message">Message to be written</param>
        public static void WriteError(Label label, string message)
        {
            if (label is LinkLabel linkLabel) linkLabel.LinkColor = ErrorColor;
            else label.ForeColor = ErrorColor;
            label.Text = message;
        }

        /// <summary>
        /// Convert number of bytes to human-readable formatted string, rounded to 1 decimal place. (e.g 79.4KB)
        /// </summary>
        /// <param name="byteCount">Number of bytes</param>
        /// <returns>Formatted string</returns>
        public static string BytesToString(int byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB" }; //int.MaxValue is in gigabyte range.
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num) + suf[place];
        }

        /// <summary>
        ///     Convert a string containing a date (in the format YYYY-MM-DD) to a DateTime.
        /// </summary>
        /// <param name="date">String to be converted</param>
        /// <returns>DateTime representing date in string</returns>
        public static DateTime StringToDate(string date)
        {
            //unreleased if date is null or doesnt have any digits (tba, n/a etc)
            if (date == null || !date.Any(Char.IsDigit)) return DateTime.MaxValue;
            int[] dateArray = date.Split('-').Select(Int32.Parse).ToArray();
            var dtDate = new DateTime();
            var dateRegex = new Regex(@"^\d{4}-\d{2}-\d{2}$");
            if (dateRegex.IsMatch(date))
            {
                //handle possible invalid dates such as february 30
                var tryDone = false;
                var tryCount = 0;
                while (!tryDone)
                {
                    try
                    {
                        dtDate = new DateTime(dateArray[0], dateArray[1], dateArray[2] - tryCount);
                        tryDone = true;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        LogToFile(
                            $"Date: {dateArray[0]}-{dateArray[1]}-{dateArray[2] - tryCount} is invalid, trying again one day earlier");
                        tryCount++;
                    }
                }
                return dtDate;
            }
            //if date only has year-month, then if month hasn't finished = unreleased
            var monthRegex = new Regex(@"^\d{4}-\d{2}$");
            if (monthRegex.IsMatch(date))
            {
                dtDate = new DateTime(dateArray[0], dateArray[1], 28);
                return dtDate;
            }
            //if date only has year, then if year hasn't finished = unreleased
            var yearRegex = new Regex(@"^\d{4}$");
            if (yearRegex.IsMatch(date))
            {
                dtDate = new DateTime(dateArray[0], 12, 28);
                return dtDate;
            }
            return DateTime.MaxValue;
        }

        /// <summary>
        ///     Convert JSON-formatted string to list of tags.
        /// </summary>
        /// <param name="tagstring">JSON-formatted string</param>
        /// <returns>List of tags</returns>
        public static List<VNItem.TagItem> StringToTags(string tagstring)
        {
            if (tagstring.Equals("")) return new List<VNItem.TagItem>();
            var curS = $"{{\"tags\":{tagstring}}}";
            var vnitem = JsonConvert.DeserializeObject<VNItem>(curS);
            return vnitem.Tags;
        }

        /// <summary>
        /// Get number of tags in specified category.
        /// </summary>
        /// <param name="tags">Collection of tags</param>
        /// <param name="category">Category that tags should be in</param>
        /// <returns>Number of tags that match</returns>
        public static int GetTagCountByCat(this IEnumerable<VNItem.TagItem> tags, TagCategory category)
        {
            switch (category)
            {
                case TagCategory.Content:
                    return tags.Count(tag => tag.Category == TagCategory.Content);
                case TagCategory.Sexual:
                    return tags.Count(tag => tag.Category == TagCategory.Sexual);
                case TagCategory.Technical:
                    return tags.Count(tag => tag.Category == TagCategory.Technical);
            }
            return 1;
        }

        /// <summary>
        ///     Decompress GZip file.
        /// </summary>
        /// <param name="fileToDecompress">File to Decompress</param>
        /// <param name="outputFile">Output File</param>
        public static void GZipDecompress(string fileToDecompress, string outputFile)
        {
            using (var originalFileStream = File.OpenRead(fileToDecompress))
            {
                var newFileName = outputFile;

                using (var decompressedFileStream = File.Create(newFileName))
                {
                    using (var decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        LogToFile($@"Decompressed: {fileToDecompress}");
                    }
                }
            }
        }

        /// <summary>
        ///     Get Days passed since date of last update.
        /// </summary>
        /// <param name="updatedDate">Date of last update</param>
        /// <returns>Number of days since last update</returns>
        public static int DaysSince(DateTime updatedDate)
        {
            if (updatedDate == DateTime.MinValue) return -1;
            return (DateTime.UtcNow - updatedDate).Days;
        }

        /// <summary>
        /// Truncates strings if they are longer than 'maxChars'.
        /// </summary>
        /// <param name="value">The string to be truncated</param>
        /// <param name="maxChars">The maximum number of characters</param>
        /// <returns>Truncated string</returns>
        public static string TruncateString(string value, int maxChars)
        {
            if (maxChars < 4) return value;
            return value.Length <= maxChars ? value : value.Substring(0, maxChars - 3) + "...";
        }

        /// <summary>
        ///     Convert DateTime to UnixTimestamp.
        /// </summary>
        /// <param name="dateTime">DateTime to be converted</param>
        /// <returns>UnixTimestamp (double)</returns>
        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (dateTime -
                    new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        /// <summary>
        ///     Saves a VN's cover image (unless it already exists)
        /// </summary>
        /// <param name="vn">VN whose image is to be saved</param>
        /// <param name="update">Get new image regardless of whether one already exists?</param>
        public static void SaveImage(VNItem vn, bool update = false)
        {
            if (!Directory.Exists(VNImagesFolder)) Directory.CreateDirectory(VNImagesFolder);
            if (vn.Image == null || vn.Image.Equals("")) return;
            string imageLocation = $"{VNImagesFolder}{vn.ID}{Path.GetExtension(vn.Image)}";
            if (File.Exists(imageLocation) && update == false) return;
            LogToFile($"Start downloading cover image for {vn}");
            try
            {
                var requestPic = WebRequest.Create(vn.Image);
                using (var responsePic = requestPic.GetResponse())
                {
                    using (var stream = responsePic.GetResponseStream())
                    {
                        if (stream == null) return;
                        var webImage = Image.FromStream(stream);
                        webImage.Save(imageLocation);
                    }
                }
            }
            catch (Exception ex)
            when (ex is NotSupportedException || ex is ArgumentNullException || ex is SecurityException || ex is UriFormatException || ex is ExternalException)
            {
                LogToFile("SaveImage Error", ex);
            }
        }

        /// <summary>
        /// Set PictureBox image as language flag or set text as language if image does not exist.
        /// </summary>
        public static void SetFlagImage(this PictureBox pictureBox, string language)
        {
            if (language == null) return;
            var prodFlag = $"{FlagsFolder}{language}.png";
            if (File.Exists(prodFlag)) pictureBox.ImageLocation = prodFlag;
            else pictureBox.Text = language;
        }

        /// <summary>
        /// Saves screenshot (by URL) to specified location.
        /// </summary>
        /// <param name="imageUrl">URL of image</param>
        /// <param name="savedLocation">Location to save image to</param>
        public static void SaveScreenshot(string imageUrl, string savedLocation)
        {
            if (!Directory.Exists(VNScreensFolder)) Directory.CreateDirectory(VNScreensFolder);
            string[] urlSplit = imageUrl.Split('/');
            if (!Directory.Exists($"{VNScreensFolder}\\{urlSplit[urlSplit.Length - 2]}"))
                Directory.CreateDirectory($"{VNScreensFolder}\\{urlSplit[urlSplit.Length - 2]}");
            if (imageUrl.Equals("")) return;
            if (File.Exists(savedLocation)) return;
            var requestPic = WebRequest.Create(imageUrl);
            using (var responsePic = requestPic.GetResponse())
            {
                using (var stream = responsePic.GetResponseStream())
                {
                    if (stream == null) return;
                    var webImage = Image.FromStream(stream);
                    webImage.Save(savedLocation);
                }
            }
        }

        /// <summary>
        ///     Delete text in label after time set in LabelFadeTime.
        /// </summary>
        /// <param name="tLabel">Label to delete text in</param>
        public static void FadeLabel(Label tLabel)
        {
            var fadeTimer = new Timer { Interval = LabelFadeTime };
            fadeTimer.Tick += (sender, e) =>
            {
                tLabel.Text = "";
                fadeTimer.Stop();
            };
            fadeTimer.Start();
        }

        /// <summary>
        ///     Saves a title's cover image (unless it already exists)
        /// </summary>
        /// <param name="vnBase">Title</param>
        public static async Task SaveImageAsync(ListedVN vnBase)
        {
            if (!Directory.Exists(VNImagesFolder)) Directory.CreateDirectory(VNImagesFolder);
            if (vnBase.ImageURL == null || vnBase.ImageURL.Equals("")) return;
            string imageLocation = $"{VNImagesFolder}{vnBase.VNID}{Path.GetExtension(vnBase.ImageURL)}";
            if (File.Exists(imageLocation)) return;
            LogToFile($"Start downloading cover image for {vnBase}");
            try
            {
                var requestPic = WebRequest.Create(vnBase.ImageURL);
                using (var responsePic = await requestPic.GetResponseAsync())
                {
                    using (var stream = responsePic.GetResponseStream())
                    {
                        if (stream == null) return;
                        var webImage = Image.FromStream(stream);
                        webImage.Save(imageLocation);
                    }
                }
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is ArgumentNullException || ex is SecurityException || ex is UriFormatException)
            {
                LogToFile("SaveImageAsync Error", ex);
            }
        }

        /// <summary>
        ///     Check if date is in the future.
        /// </summary>
        /// <param name="date">Date to be checked</param>
        /// <returns>Whether date is in the future</returns>
        public static bool DateIsUnreleased(string date)
        {
            return StringToDate(date) > DateTime.UtcNow;
        }

        /// <summary>
        /// Get path of stored screenshot (Doesn't check if it exists).
        /// </summary>
        public static string StoredLocation(this VNItem.ScreenItem screenItem)
        {
            string[] urlSplit = screenItem.Image.Split('/');
            return $"{VNScreensFolder}{urlSplit[urlSplit.Length - 2]}\\{urlSplit[urlSplit.Length - 1]}";
        }
    }
}
