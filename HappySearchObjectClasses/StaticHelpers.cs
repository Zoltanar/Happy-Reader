using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Happy_Apps_Core.Database;
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
		public const string ProjectURL = "https://github.com/Zoltanar/Happy-Reader";
		public static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Happy Reader");
		public static readonly string StoredDataFolder = Path.Combine(AppDataFolder, "Stored Data");
		public static readonly string LogsFolder = Path.Combine(AppDataFolder, "Logs");
		public static readonly string ProgramDataFolder = "Program Data";

		public static readonly string DefaultTraitsJson = Path.Combine(ProgramDataFolder,"Default Files\\traits.json");
		public static readonly string DefaultTagsJson = Path.Combine(ProgramDataFolder, "Default Files\\tags.json");
		public static readonly string NsfwImageFile = Path.Combine(ProgramDataFolder, "Default Files\\nsfw-image.png");
		public static readonly string NoImageFile =  Path.Combine(ProgramDataFolder, "Default Files\\no-image.png");
		public static readonly string FlagsFolder = Path.Combine(ProgramDataFolder, "Flags\\");

		public static readonly string ImagesFolder = Path.Combine(StoredDataFolder, "vndb-img\\");/*
		public static readonly string CoverImagesFolder = Path.Combine(StoredDataFolder, "vndb-img\\cv\\");
		public static readonly string VNScreensFolder = Path.Combine(StoredDataFolder, "vndb-img\\sf\\");
		public static readonly string CharacterImagesFolder = Path.Combine(StoredDataFolder, "vndb-img\\ch\\");*/
		public static readonly string DBStatsJson = Path.Combine(StoredDataFolder, "dbs.json");
		public static readonly string SettingsJson = Path.Combine(StoredDataFolder, "settings.json");
		public static readonly string CoreSettingsJson = Path.Combine(StoredDataFolder, "coresettings.json");
		public static readonly string DatabaseFile = Path.Combine(StoredDataFolder, "Happy-Apps.sqlite");
#pragma warning restore 1591
		#endregion

#pragma warning disable 1591
		public const string ClientName = "Happy Reader";
		public const string ClientVersion = "2.0.0";
		public const string APIVersion = "2.25";
		public const int APIMaxResults = 25;
		public static readonly string MaxResultsString = "\"results\":" + APIMaxResults;
		
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
		public static readonly MultiLogger Logger;
		public static VndbConnection Conn;
#pragma warning restore 1591

		static StaticHelpers()
		{
			Directory.CreateDirectory(StoredDataFolder);
			CSettings = SettingsJsonFile.Load<CoreSettings>(CoreSettingsJson);
			Logger = new MultiLogger(LogsFolder);
		}

		public static VisualNovelDatabase LocalDatabase;

		public static bool VNIsByFavoriteProducer(ListedVN vn)
		{
			if (!vn.ProducerID.HasValue) return false;
			int tries = 0;
			Exception exception;
			do
			{
				tries++;
				try
				{
					return LocalDatabase.UserProducers[(vn.ProducerID.Value, LocalDatabase.CurrentUser.Id)] != null;
					//return LocalDatabase.CurrentUser.FavoriteProducers.Any(x => x.ID == vn.ProducerID); 
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
			try
			{
				FieldInfo field = value.GetType().GetField(value.ToString());
				Attribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
				return attribute is DescriptionAttribute descriptionAttribute
					? descriptionAttribute.Description
					: value.ToString();
			}
			catch (Exception ex)
			{
				Logger.ToFile(ex);
				return value.ToString();
			}
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
			if (date == null || !date.Any(char.IsDigit)) return DateTime.MaxValue;
			int[] dateArray = date.Split('-').Select(int.Parse).ToArray();
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

		public static DateTime Epoch { get; } = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		///     Convert DateTime to UnixTimestamp.
		/// </summary>
		/// <param name="dateTime">DateTime to be converted</param>
		/// <returns>UnixTimestamp (double)</returns>
		public static double ToUnixTimestamp(this DateTime dateTime)
		{
			return (dateTime - Epoch).TotalSeconds;
		}

		/// <summary>
		/// Convert DateTime to UnixTimestamp, if date time is null, returns null.
		/// </summary>
		/// <param name="dateTime">DateTime to be converted</param>
		/// <returns>UnixTimestamp (int)</returns>
		public static int? ToUnixTimestamp(this DateTime? dateTime)
		{
			if (!dateTime.HasValue) return null;
			return (int) ToUnixTimestamp(dateTime.Value);
		}

		/// <summary>
		/// Convert Unix Timestamp to date time, if timestamp is null, returns null.
		/// </summary>
		public static DateTime? UnixTimestampToDateTime(this int? timestamp)
		{
			if (!timestamp.HasValue) return null;
			return Epoch.AddSeconds(timestamp.Value);
		}
		public static string ToSeconds(this TimeSpan time) => $"{time.TotalSeconds:N0}.{time.Milliseconds} seconds";

		private const string PasswordRegistryKey = "SOFTWARE\\" + ClientName;
		private const string PasswordRegistryCipherValueName = "Data1";
		private const string PasswordRegistryEntropyValueName = "Data2";

		/// <summary>
		///     Save user's VNDB login password to Windows Registry (encrypted).
		/// </summary>
		/// <param name="password">User's password</param>
		public static void SavePassword(char[] password)
		{
			//prepare data
			byte[] stringBytes = Encoding.UTF8.GetBytes(password);
			var entropy = new byte[20];
			using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(entropy);
			byte[] cipherText = ProtectedData.Protect(stringBytes, entropy, DataProtectionScope.CurrentUser);
			//store data
			var key = Registry.CurrentUser.OpenSubKey(PasswordRegistryKey, true) ??
			          Registry.CurrentUser.CreateSubKey(PasswordRegistryKey, true) ??
				throw new ArgumentNullException(@"key",$"Registry key not found/created: {PasswordRegistryKey}");
			key.SetValue(PasswordRegistryCipherValueName, cipherText);
			key.SetValue(PasswordRegistryEntropyValueName, entropy);
			key.Close();
			Logger.ToFile("Saved Login Password");
		}

		/// <summary>
		/// Load user's VNDB login credentials from Windows Registry
		/// </summary>
		public static char[] LoadPassword()
		{
			//get key data
			var key = Registry.CurrentUser.OpenSubKey(PasswordRegistryKey);
			if (key == null) return null;
			var password = key.GetValue(PasswordRegistryCipherValueName) as byte[];
			var entropy = key.GetValue(PasswordRegistryEntropyValueName) as byte[];
			key.Close();
			if (password == null || entropy == null) return null;
			byte[] passwordBytes = ProtectedData.Unprotect(password, entropy, DataProtectionScope.CurrentUser);
			Logger.ToFile("Loaded Login Password");
			return Encoding.UTF8.GetChars(passwordBytes);
		}

		public static void AddParameter(this DbCommand command, string parameterName, object value)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value;
			command.Parameters.Add(parameter);
		}

		public static DateTime? GetNullableDate(object dbObject)
		{
			return dbObject == DBNull.Value ? (DateTime?) null : Convert.ToDateTime(dbObject);
		}

		public static int? GetNullableInt(object dbObject)
		{
			return dbObject == DBNull.Value ? (int?) null : Convert.ToInt32(dbObject);
		}

		public static string GetNullableString(object dbObject)
		{
			return dbObject == DBNull.Value ? null : Convert.ToString(dbObject);
		}

		public static bool DownloadFile(string uri, string destinationFolderPath, string destinationFileName, out string destinationFilePath)
		{
			WebResponse response = null;
			Stream stream = null;
			Stream destinationStream = null;
			destinationFilePath = null;
			try
			{
				Logger.ToFile($"Starting download of {uri}");
				// ReSharper disable once AssignNullToNotNullAttribute
				Directory.CreateDirectory(Path.GetDirectoryName(destinationFolderPath));
				var request = WebRequest.Create(uri);
				response = request.GetResponse();
				stream = response.GetResponseStream();
				if (stream == null) return false;
				if (string.IsNullOrWhiteSpace(destinationFileName))
				{
					if (!string.IsNullOrEmpty(response.Headers["Content-Disposition"]))
					{
						destinationFileName = response.Headers["Content-Disposition"]
							.Substring(response.Headers["Content-Disposition"].IndexOf("filename=", StringComparison.Ordinal) + 10).Replace("\"", "");
					}
					else destinationFileName = Path.GetFileName(response.ResponseUri.AbsolutePath);
				}
				destinationFilePath = Path.Combine(destinationFolderPath, destinationFileName);
				if (File.Exists(destinationFilePath)) return true;
				destinationStream = File.OpenWrite(destinationFilePath);
				Logger.ToFile($"Downloading to {destinationFilePath}");
				stream.CopyTo(destinationStream);
				return true;
			}
			catch (Exception ex) when (ex is NotSupportedException || ex is ArgumentNullException ||
			                           ex is SecurityException || ex is UriFormatException || ex is ExternalException)
			{
				Logger.ToFile(ex);
				return false;
			}
			finally
			{
				response?.Dispose();
				stream?.Dispose();
				destinationStream?.Dispose();
			}
		}

		public static void RunWithRetries(Action action, Action onFailure, int maxAtempts, Func<Exception, bool> exceptionValid)
		{
			int attempts = 0;
			while (attempts < maxAtempts)
			{
				try
				{
					action();
					break;
				}
				catch (Exception ex)
				{
					if (!exceptionValid(ex) || attempts == maxAtempts-1) throw;
					onFailure?.Invoke();
					attempts++;
				}
			}
		}

		public static string GetImageLocation(string imageId2)
		{
			var folder = imageId2.Substring(0, 2);
			var id = int.Parse(imageId2.Substring(2));
			var filePath = Path.GetFullPath($"{ImagesFolder}\\{folder}\\{id % 100:00}\\{id}.jpg");
			return filePath;
		}
	}
}
