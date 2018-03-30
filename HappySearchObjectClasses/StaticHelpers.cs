using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Happy_Apps_Core.Database;
using System.Windows.Media;

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
        public const string NsfwImageFile = "Program Data\\Default Files\\nsfw-image.png";
        public const string NoImageFile = "Program Data\\Default Files\\no-image.png";
        public const string FlagsFolder = "Program Data\\Flags\\";

        public const string
            StoredDataFolder =
                @"..\..\Stored Data\"; //this is in order to use same folder for all builds (32/64 and debug/release)

        public const string VNImagesFolder = StoredDataFolder + "Saved Cover Images\\";
        public const string VNScreensFolder = StoredDataFolder + "Saved Screenshots\\";
        public const string DBStatsJson = StoredDataFolder + "dbs.json";
        public const string SettingsJson = StoredDataFolder + "settings.json";
        public const string CustomFiltersJson = StoredDataFolder + "customfilters.json";
        public const string PermanentFilterJson = StoredDataFolder + "filters.json";
        public const string LogFile = StoredDataFolder + "message.log";
        public const string CoreSettingsJson = StoredDataFolder + "coresettings.json";
        public const string GuiSettingsJson = StoredDataFolder + "guisettings.json";
#pragma warning restore 1591

        #endregion

#pragma warning disable 1591
        public const string ClientName = "Happy Reader";
        public const string ClientVersion = "2.0.0";
        public const string APIVersion = "2.25";
        public const int APIMaxResults = 25;
        public static readonly string MaxResultsString = "\"results\":" + APIMaxResults;

        //tile background colors
        public static readonly Brush DefaultTileBrush = Brushes.LightBlue;
        public static readonly Brush WLHighBrush = Brushes.DeepPink;
        public static readonly Brush WLMediumBrush = Brushes.HotPink;
        public static readonly Brush WLLowBrush = Brushes.LightPink;
        public static readonly Brush ULFinishedBrush = Brushes.LightGreen;
        public static readonly Brush ULStalledBrush = Brushes.DarkKhaki;
        public static readonly Brush ULDroppedBrush = Brushes.DarkOrange;
        public static readonly Brush ULUnknownBrush = Brushes.Gray;

        // ReSharper disable UnusedMember.Local
        //tile text colors
        public static readonly Brush FavoriteProducerBrush = Brushes.Yellow;
        public static readonly Brush ULPlayingBrush = Brushes.Yellow;
        public static readonly Brush UnreleasedBrush = Brushes.White;

        //text colors
        private static readonly Color NormalColor = Colors.White;
        private static readonly Color WarningColor = Colors.DarkKhaki;
        private static readonly Color ErrorColor = Colors.Red;

        /// <summary>
        /// Categories of VN Tags
        /// </summary>
        public enum TagCategory
        {
            Null,
            Content,
            Sexual,
            Technical
        }

        public static readonly CoreSettings CSettings;
        public static readonly GuiSettings GSettings;

        public static VndbConnection Conn;
#pragma warning restore 1591

        static StaticHelpers()
        {
            Directory.CreateDirectory(StoredDataFolder);
            File.Create(LogFile).Close();
            CSettings = SettingsJsonFile.Load<CoreSettings>(CoreSettingsJson);
            GSettings = SettingsJsonFile.Load<GuiSettings>(GuiSettingsJson);
        }

        public static VisualNovelDatabase LocalDatabase;

        public static bool VNIsByFavoriteProducer(ListedVN vn) => LocalDatabase.CurrentUser.FavoriteProducers.Any(x => x.ID == vn.ProducerID);

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
        /// <param name="logDebug">Print to debug, true by default</param>
        public static void LogToFile(string message, bool logDebug = true)
        {
            if(logDebug) Debug.Print(message);
            int counter = 0;
            while (IsFileLocked(new FileInfo(LogFile)))
            {
                counter++;
                if (counter > 5) throw new IOException("Logfile is locked!");
                Thread.Sleep(25);
            }
            using (var writer = new StreamWriter(LogFile, true)) writer.WriteLine(message);
        }

        /// <summary>
        /// Print exception to Debug and write it to log file.
        /// </summary>
        /// <param name="exception">Exception to be written to file</param>
        /// <param name="source">Source of error, CallerMemberName by default</param>
        public static void LogToFile(Exception exception, [CallerMemberName] string source = null)
        {
            Debug.Print($"Source: {source}");
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
                writer.WriteLine($"Source: {source}");
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
            if (value == null) return "N/A";
            FieldInfo field = value.GetType().GetField(value.ToString());
            return !(Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute
            )
                ? value.ToString()
                : attribute.Description;
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
        public static int DaysSince(this DateTime updatedDate)
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
            if (value == null || maxChars < 4) return value;
            return value.Length <= maxChars ? value : value.Substring(0, maxChars - 3) + "...";
        }

        /// <summary>
        ///     Convert DateTime to UnixTimestamp.
        /// </summary>
        /// <param name="dateTime">DateTime to be converted</param>
        /// <returns>UnixTimestamp (double)</returns>
        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

		[Conditional("LOGVERBOSE")]
	    public static void LogVerbose(string text)
	    {
		    Debug.WriteLine(text);
	    }
    }
}
