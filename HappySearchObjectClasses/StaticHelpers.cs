using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Happy_Apps_Core.Database;
using System.Windows.Media;
using Microsoft.Win32;

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
		public const string StoredDataFolder = @"..\Stored Data\"; //this is to use same folder for debug/release builds
		public const string VNImagesFolder = StoredDataFolder + "Saved Cover Images\\";
		public const string VNScreensFolder = StoredDataFolder + "Saved Screenshots\\";
		public const string DBStatsJson = StoredDataFolder + "dbs.json";
		public const string SettingsJson = StoredDataFolder + "settings.json";
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
		public static readonly Brush WLBlacklistBrush = Brushes.Black;
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
		public static readonly MultiLogger Logger;
		public static VndbConnection Conn;
#pragma warning restore 1591

		static StaticHelpers()
		{
			Directory.CreateDirectory(StoredDataFolder);
			File.Create(LogFile).Close();
			CSettings = SettingsJsonFile.Load<CoreSettings>(CoreSettingsJson);
			GSettings = SettingsJsonFile.Load<GuiSettings>(GuiSettingsJson);
			Logger = new MultiLogger(LogFile);
		}

		public static VisualNovelDatabase LocalDatabase;

		public static bool VNIsByFavoriteProducer(ListedVN vn)
		{
			int tries = 0;
			Exception exception;
			do
			{
				tries++;
				try
				{
					return LocalDatabase.CurrentUser.FavoriteProducers.Any(x => x.ID == vn.ProducerID);
				}
				catch (InvalidOperationException ex)
				{
					exception = ex;
					if (ex.TargetSite.Name != nameof(IEnumerator.MoveNext)) throw;
				}
			} while (tries < 5);
			throw exception;
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
		/// Gets description of Enum value if available.
		/// </summary>
		public static string GetDescription(this Enum value)
		{
			if (value == null) return "N/A";
			FieldInfo field = value.GetType().GetField(value.ToString());
			Attribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
			return attribute is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : value.ToString();
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

		//public static DateTime StringToDate()

		/// <summary>
		/// Convert a string containing a date (in the format YYYY-MM-DD) to a DateTime.
		/// </summary>
		/// <param name="date">String to be converted</param>
		/// <param name="hasFullDate">Boolean indicating that release date string had all components of a date, and not just year, or year-month.</param>
		/// <returns>DateTime representing date in string</returns>
		public static DateTime StringToDate(string date, out bool hasFullDate)
		{
			hasFullDate = false;
			//unreleased if date is null or doesnt have any digits (tba, n/a etc)
			if (date == null || !date.Any(Char.IsDigit)) return DateTime.MaxValue;
			int[] dateArray = date.Split('-').Select(Int32.Parse).ToArray();
			var dtDate = new DateTime();
			var dateRegex = new Regex(@"^\d{4}-\d{2}-\d{2}$");
			if (dateRegex.IsMatch(date))
			{
				hasFullDate = true;
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
						Logger.ToFile(
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
						Logger.ToFile($@"Decompressed: {fileToDecompress}");
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

		public static string ToSeconds(this TimeSpan time) => $"{time.TotalSeconds:N0}.{time.Milliseconds} seconds";

		/// <summary>
		///     Save user's VNDB login password to Windows Registry (encrypted).
		/// </summary>
		/// <param name="password">User's password</param>
		public static void SaveCredentials(char[] password)
		{
			//prepare data
			byte[] stringBytes = Encoding.UTF8.GetBytes(password);

			var entropy = new byte[20];
			using (var rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(entropy);
			}
			byte[] cipherText = ProtectedData.Protect(stringBytes, entropy,
				DataProtectionScope.CurrentUser);

			//store data
			var key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{ClientName}", true) ??
			          Registry.CurrentUser.CreateSubKey($"SOFTWARE\\{ClientName}", true);
			if (key == null) return;
			key.SetValue("Data1", cipherText);
			key.SetValue("Data2", entropy);
			key.Close();
			Logger.ToFile("Saved Login Password");
		}

		/// <summary>
		///     Load user's VNDB login credentials from Windows Registry
		/// </summary>
		public static char[] LoadPassword()
		{
			//get key data
			var key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{ClientName}");
			if (key == null) return null;
			var password = key.GetValue("Data1") as byte[];
			var entropy = key.GetValue("Data2") as byte[];
			key.Close();
			if (password == null || entropy == null) return null;
			byte[] vv = ProtectedData.Unprotect(password, entropy, DataProtectionScope.CurrentUser);
			Logger.ToFile("Loaded Login Password");
			return Encoding.UTF8.GetChars(vv);
		}
	}
}
