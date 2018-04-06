using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Happy_Apps_Core;
using Happy_Reader.Database;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using IthVnrSharpLib;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

// ReSharper disable UnusedMember.Global

namespace Happy_Reader
{
	public static class StaticMethods
	{
		public delegate void NotificationEventHandler(object sender, string message, string title = null);

		private const string ConfigFolder = "Config\\";
		public const string ProxiesJson = ConfigFolder + "proxies.json";
		public const string CustomFiltersJson = StaticHelpers.StoredDataFolder + "customfilters.json";
		public const string PermanentFilterJson = StaticHelpers.StoredDataFolder + "filters.json";
		public static HappyReaderDatabase Data { get; } = new HappyReaderDatabase();

		public static VNR VnrProxy { get; private set; }
		public static AppDomain IthVnrDomain { get; private set; }
		public static GuiSettings GSettings => StaticHelpers.GSettings;

		static StaticMethods()
		{
			try
			{
				Directory.CreateDirectory(ConfigFolder);
				InitVnrProxy();
			}
			catch (Exception ex)
			{
				StaticHelpers.LogToFile(ex);

			}
		}
		
		public static void InitVnrProxy()
		{
			var path = Path.GetFullPath(@"IthVnrSharpLib.dll");
			var assembly = Assembly.LoadFile(path);
			IthVnrDomain = AppDomain.CreateDomain(@"StaticMethodsIthVnrAppDomain");
			VnrProxy = (VNR)IthVnrDomain.CreateInstanceAndUnwrap(assembly.FullName, @"IthVnrSharpLib.VNR");
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
			process.WaitForInputIdle(3000);
			return process;
		}

		public static Process StartProcessThroughProxy(UserGame userGame)
		{
			var lastQuote = userGame.LaunchPath.LastIndexOf('"');
			var proxyPath = userGame.LaunchPath.Substring(1, lastQuote - 1);
			var args = userGame.LaunchPath.Substring(lastQuote + 1).Trim();
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(userGame.FilePath));
			Process existing = processes.FirstOrDefault();
			if (existing != null) return existing;
			string exeParentFolder = Path.GetDirectoryName(proxyPath);
			// ReSharper disable once NotResolvedInText
			if (exeParentFolder == null) throw new ArgumentNullException("exeParentFolder", "Parent folder of exe was not found.");
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

		public static void SaveTranslationCache()
		{
			Debug.WriteLine($"GoogleTranslate: Got from cache {HRGoogleTranslate.GoogleTranslate.GotFromCacheCount}");
			Debug.WriteLine($"GoogleTranslate: Got from API {HRGoogleTranslate.GoogleTranslate.GotFromAPICount}");
			Debug.WriteLine("Saving Translation Cache...");
			var rows = Data.SaveChanges();
			Debug.WriteLine($"Finished saving translation cache ({rows} entries).");
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
	}
}
