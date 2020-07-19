using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Happy_Apps_Core;
using Happy_Reader.Database;
using System.Linq.Expressions;
using System.Management;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using Happy_Apps_Core.Database;
using HRGoogleTranslate;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

// ReSharper disable UnusedMember.Global

namespace Happy_Reader
{
	public static class StaticMethods
	{
		public delegate void NotificationEventHandler(object sender, string message, string title = null);

		public static readonly string ProxiesJson = Path.Combine(StaticHelpers.StoredDataFolder, "proxies.json");
		public static readonly string CustomFiltersJson = Path.Combine(StaticHelpers.StoredDataFolder, "customfilters.json");
		public static readonly string PermanentFilterJson = Path.Combine(StaticHelpers.StoredDataFolder, "filters.json");
		public static readonly string GuiSettingsJson = Path.Combine(StaticHelpers.StoredDataFolder, "guisettings.json");
		public static readonly string TranslatorSettingsJson = Path.Combine(StaticHelpers.StoredDataFolder, "translatorsettings.json");
		public static HappyReaderDatabase Data { get; } = new HappyReaderDatabase();
		public static readonly GuiSettings GSettings;
		public static readonly TranslatorSettings TSettings;

		static StaticMethods()
		{
			GSettings = SettingsJsonFile.Load<GuiSettings>(GuiSettingsJson);
			TSettings = SettingsJsonFile.Load<TranslatorSettings>(TranslatorSettingsJson);
		}

		public static string GetLocalizedTime(this DateTime dateTime)
		{
			bool isAmPm = GSettings.CultureInfo.DateTimeFormat.AMDesignator != String.Empty;
			return dateTime.ToString(isAmPm ? "hh:mm tt" : "HH:mm", GSettings.CultureInfo);
		}

		public static Process StartProcess(string executablePath)
		{
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executablePath));
			Process existing = processes.FirstOrDefault();
			if (existing != null) return existing;
			string exeParentFolder = Path.GetDirectoryName(executablePath);
			// ReSharper disable once NotResolvedInText
			if (exeParentFolder == null) throw new ArgumentNullException("exeParentFolder", "Parent folder of exe was not found.");
			ProcessStartInfo pi = new ProcessStartInfo
			{
				FileName = executablePath,
				UseShellExecute = false,
				WorkingDirectory = exeParentFolder
			};
			var process = Process.Start(pi);
			Debug.Assert(process != null, nameof(process) + " != null");
			if (!process.HasExited) process.WaitForInputIdle(3000);
			return process;
		}

		public static Process StartProcessThroughProxy(UserGame userGame)
		{
			var firstQuote = userGame.LaunchPath.IndexOf('"');
			string proxyPath;
			string args;
			if (firstQuote > -1)
			{
				var secondQuote = userGame.LaunchPath.IndexOf('"', firstQuote+1);
				proxyPath = userGame.LaunchPath.Substring(firstQuote+1, secondQuote - 1);
				args = userGame.LaunchPath.Substring(secondQuote+1).Trim();
			}
			else
			{
				var firstSpace = userGame.LaunchPath.IndexOf(' ');
				proxyPath = userGame.LaunchPath.Substring(0,firstSpace);
				args = userGame.LaunchPath.Substring(firstSpace+1).Trim();
			}
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(userGame.FilePath));
			Process existing = processes.FirstOrDefault();
			if (existing != null) return existing;
			string exeParentFolder = Path.GetDirectoryName(proxyPath);
			// ReSharper disable once NotResolvedInText
			if (exeParentFolder == null) throw new ArgumentNullException(nameof(exeParentFolder), "Parent folder of exe was not found.");
			ProcessStartInfo pi = new ProcessStartInfo
			{
				FileName = proxyPath,
				UseShellExecute = false,
				WorkingDirectory = exeParentFolder,
				Arguments = args
			};
			Process.Start(pi);
			Thread.Sleep(1500);
			processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(userGame.FilePath));
			existing = processes.FirstOrDefault();
			return existing;
		}

		public static Process StartProcessThroughLocaleEmulator(UserGame userGame)
		{
			var proxyPath = StaticMethods.GSettings.LocaleEmulatorPath;
			var args = $"\"{userGame.FilePath}\"";
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(userGame.FilePath));
			Process existing = processes.FirstOrDefault();
			if (existing != null) return existing;
			string exeParentFolder = Path.GetDirectoryName(proxyPath);
			// ReSharper disable once NotResolvedInText
			if (exeParentFolder == null) throw new ArgumentNullException(nameof(exeParentFolder), "Parent folder of exe was not found.");
			ProcessStartInfo pi = new ProcessStartInfo
			{
				FileName = proxyPath,
				UseShellExecute = false,
				WorkingDirectory = exeParentFolder,
				Arguments = args
			};
			Process.Start(pi);
			Thread.Sleep(1500);
			processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(userGame.FilePath));
			existing = processes.FirstOrDefault();
			return existing;
		}

		public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
		{
			while (true)
			{
				//get parent item
				DependencyObject parentObject = VisualTreeHelper.GetParent(child);

				//we've reached the end of the tree
				if (parentObject == null) return null;

				//check if the parent matches the type we're looking for
				if (parentObject is T parent) return parent;
				child = parentObject;
			}
		}

		public static Collection<T> GetVisualChildren<T>(DependencyObject current) where T : DependencyObject
		{
			if (current == null)
				return null;

			var children = new Collection<T>();
			GetVisualChildren(current, children);
			return children;
		}

		private static void GetVisualChildren<T>(DependencyObject current, Collection<T> children) where T : DependencyObject
		{
			if (current == null) return;
			if (current.GetType() == typeof(T)) children.Add((T)current);

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
			{
				GetVisualChildren(VisualTreeHelper.GetChild(current, i), children);
			}
		}

		public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (!dictionary.ContainsKey(key)) dictionary.Add(key, value);
			try
			{
				return dictionary[key];
			}
			catch
			{
				return default;
			}
		}

		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
		{
			return dict.TryGetValue(key, out var result) ? result : defaultValue;
		}

		public static bool Is64BitProcess(this Process process)
		{
			if (!Environment.Is64BitOperatingSystem) return false;
			bool isWow64Process;
			try
			{
				if (!NativeMethods.IsWow64Process(process.Handle, out isWow64Process))
				{
					// ReSharper disable once UnusedVariable
					var error = Marshal.GetLastWin32Error();
					return true;
				}
			}
			catch (Win32Exception) { return true; }
			return !isWow64Process;
		}

		public static bool UserIsSure(string message = "Are you sure?")
			=> MessageBox.Show(message, "Happy Reader - Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes;

		public static Process GetClipboardOwner()
		{
			var handle = NativeMethods.GetClipboardOwner();
			if (handle == IntPtr.Zero) return null;
			NativeMethods.GetWindowThreadProcessId(handle, out uint pid);
			return pid == 0 ? null : Process.GetProcessById((int)pid);
		}

		public static NativeMethods.RECT GetWindowDimensions(Process process)
		{
			var windowHandle = process.MainWindowHandle;
			var rct = new NativeMethods.RECT();
			NativeMethods.GetWindowRect(windowHandle, ref rct);
			if (rct.ZeroSized)
			{
				var hwnd = NativeMethods.FindWindow(null, process.MainWindowTitle);
				NativeMethods.GetWindowRect(hwnd, ref rct);
			}
			return rct;
		}

		public static void ExitTranslation()
		{
			Debug.WriteLine($"GoogleTranslate: Got from cache {GoogleTranslate.GotFromCacheCount}");
			Debug.WriteLine($"GoogleTranslate: Got from API {GoogleTranslate.GotFromAPICount}");
			Data.SaveChanges();
		}

		/// <summary>
		/// NanUnion is a C++ style type union used for efficiently converting
		/// a double into an unsigned long, whose bits can be easily manipulated</summary>
		[StructLayout(LayoutKind.Explicit)]
		private struct NanUnion
		{
			/// <summary>
			/// Floating point representation of the union</summary>
			[FieldOffset(0)]
			internal double FloatingValue;

			/// <summary>
			/// Integer representation of the union</summary>
			[FieldOffset(0)]
			// ReSharper disable once FieldCanBeMadeReadOnly.Local
			internal ulong IntegerValue;
		}

		/// <summary>
		/// Check if a number isn't really a number</summary>
		/// <param name="value">The number to check</param>
		/// <returns>True if the number is not a number, false if it is a number</returns>
		public static bool IsNaN(this double value)
		{
			// Get the double as an unsigned long
			NanUnion union = new NanUnion { FloatingValue = value };

			// An IEEE 754 double precision floating point number is NaN if its
			// exponent equals 2047 and it has a non-zero mantissa.
			ulong exponent = union.IntegerValue & 0xfff0000000000000L;
			if ((exponent != 0x7ff0000000000000L) && (exponent != 0xfff0000000000000L))
			{
				return false;
			}
			ulong mantissa = union.IntegerValue & 0x000fffffffffffffL;
			return mantissa != 0L;
		}

		/// <summary>
		/// Truncates strings if they are longer than 'maxChars' (minimum is 4 characters).
		/// </summary>
		/// <param name="maxChars">The maximum number of characters</param>
		/// <returns>Truncated string</returns>
		public static Expression<Func<string, string>> TruncateStringExpression(int maxChars)
		{
			maxChars = Math.Max(maxChars, 4);
			Expression<Func<string, string>> expression = x => x == null ? null :
				(x.Length <= maxChars ? x : x.Substring(0, maxChars - 3) + "...");
			return expression;
		}

		public static readonly Func<string, string> TruncateStringFunction30 = TruncateStringExpression(30).Compile();

		public static string ToHumanReadable(this TimeSpan timeSpan)
		{
			if (timeSpan.TotalMinutes < 1) return $"{timeSpan.Seconds} seconds";
			if (Math.Abs(timeSpan.TotalMinutes - 1) < 0.01) return "1 minute";
			if (timeSpan.TotalMinutes > 1 && timeSpan.TotalMinutes < 60) return $"{timeSpan.Minutes} minutes, {timeSpan.Seconds} seconds";
			if (Math.Abs(timeSpan.TotalMinutes - 60) < 0.01) return "1 hour";
			if (timeSpan.TotalHours > 1 && timeSpan.TotalHours < 2) return $"1 hour, {timeSpan.Minutes} minutes";
			return $"{(int)timeSpan.TotalHours} hours, {timeSpan.Minutes} minutes";
		}

		public static ComboBoxItem[] GetEnumValues(Type enumType)
		{
			var result = Enum.GetValues(enumType);
			return result.Length == 0 ? new ComboBoxItem[0] : result.Cast<Enum>().Select(x => new ComboBoxItem
			{
				Content = x.GetDescription(), 
				Tag = x, 
				HorizontalContentAlignment = HorizontalAlignment.Left, 
				VerticalContentAlignment = VerticalAlignment.Center
			}).ToArray();
		}

		//todo make this an externally loaded list
		public static HashSet<string> ExcludedNamesForVNResolve = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"data",
			"windows-i686",
			"lib",
			"update.exe",
			"install.exe",
			"ihs.exe",
			"lcsebody",
			"セーブデータフォルダを開く.exe",
			"savedata",
			"cg",
			"patch",
			"plugin"
		};

		public static readonly string[] ResolveNamesToSkip =
		{
		};

		public static ListedVN ResolveVNForFile(string file)
		{
			var filename = Path.GetFileNameWithoutExtension(file);
			ListedVN[] fileResults = StaticHelpers.LocalDatabase.VisualNovels.Where(VisualNovelDatabase.SearchForVN(filename)).ToArray();
			ListedVN vn = null;
			if (fileResults.Length == 1 && !ExcludedNamesForVNResolve.Contains(Path.GetFileName(file))) vn = fileResults.First();
			else
			{
				var parent = Directory.GetParent(file);
				//if parent is not null, and if parent's parent  is not null (if parent's parent is null, then it is the root location.
				while (parent?.Parent != null)
				{
					if (ExcludedNamesForVNResolve.Contains(parent.Name))
					{
						parent = parent.Parent;
						continue;
					}
					ListedVN[] folderResults = StaticHelpers.LocalDatabase.VisualNovels.Where(VisualNovelDatabase.SearchForVN(parent.Name)).ToArray();
					if (folderResults.Length == 1)
					{
						vn = folderResults.First();
						break;
					}
					parent = parent.Parent;
				}
			}
			//ListedVN[] allResults = fileResults.Concat(folderResults).ToArray(); //todo list results and ask user
			return vn;
		}

		/// <summary>
		/// Returns dictionary with Key: VN, Value: List of paths.
		/// </summary>
		public static Dictionary<ListedVN, List<string>> GetMissingVNsFromFolder(string folderPath)
		{
			var allExes = new DirectoryInfo(folderPath).GetFiles("*.exe", SearchOption.AllDirectories);
			var foundGames = new Dictionary<ListedVN, List<string>>();
			foreach (var file in allExes)
			{
				if (Data.UserGames.Any(ug => ug.FilePath == file.FullName)
				|| ExcludedNamesForVNResolve.Contains(file.Name) || file.Name.ToLower().Contains("uninst")) continue;
				var vn = ResolveVNForFile(file.FullName);
				if (vn == null || vn.IsOwned == OwnedStatus.CurrentlyOwned) continue;
				if (!foundGames.ContainsKey(vn)) foundGames[vn] = new List<string>();
				foundGames[vn].Add(file.FullName);
			}
			foreach (var pair in foundGames)
			{
				StaticHelpers.Logger.ToFile($"Found {pair.Value.Count} titles for VN {pair.Key}");
				foreach (var path in pair.Value)
				{
					StaticHelpers.Logger.ToFile($"\t{path}");
				}
			}
			return foundGames;
		}

		public static void GetFoldersWithoutVNs(string folderPath, int depth)
		{
			if (depth > 2 || depth < 1) throw new ArgumentOutOfRangeException(nameof(depth));
			var allFolders = new DirectoryInfo(folderPath).GetDirectories();
			StaticHelpers.Logger.ToFile($"Folders in '{folderPath}' without VNs, depth {depth}:");
			if (depth == 2)
			{
				allFolders = allFolders.Where(folder =>
				{
					var allExes = folder.GetFiles("*.exe", SearchOption.TopDirectoryOnly).Select(f => f.FullName).ToArray();
					if (Data.UserGames.Any(ug => allExes.Contains(ug.FilePath))) return false;
					return true;
				}).SelectMany(f => f.GetDirectories()).ToArray();
			}

			allFolders = allFolders.Where(f => !ExcludedNamesForVNResolve.Contains(f.Name)).ToArray();
			foreach (var folder in allFolders)
			{
				var allExes = folder.GetFiles("*.exe", SearchOption.AllDirectories).Select(f => f.FullName).ToArray();
				if (Data.UserGames.Any(ug => allExes.Contains(ug.FilePath))) continue;
				StaticHelpers.Logger.ToFile($"\t{folder.FullName}");
			}
		}

		public static Dictionary<string, FontFamily> FontsInstalled = 
			//select all font families
			Fonts.SystemFontFamilies
				//for each font family
				.SelectMany(ff =>
						//select unique font names
						ff.FamilyNames.GroupBy(fn => fn.Value).Select(fng => fng.First())
						//create map of font name to font family (for this font family)
						.Select(f => new KeyValuePair<string, FontFamily>(f.Value, ff))) 
			//create dictionary of unique font name to font family
			.ToDictionary(f => f.Key, f => f.Value);


		public static string GetProcessFileName(Process process)
		{
			string processFileName;
			if (process.Is64BitProcess())
			{
				var searcher = new ManagementObjectSearcher("root\\CIMV2", $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {process.Id}");
				processFileName = searcher.Get().Cast<ManagementObject>().FirstOrDefault()?["ExecutablePath"].ToString() ?? String.Empty;
				if (String.IsNullOrWhiteSpace(processFileName)) throw new InvalidOperationException("Did not find executable path for process");
			}
			else
			{
				Trace.Assert(process.MainModule != null, "gameProcess.MainModule != null");
				processFileName = process.MainModule.FileName;
			}
			return processFileName;
		}
	}
}
