using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using Happy_Apps_Core;
using Happy_Reader.Database;
using System.Linq.Expressions;
// ReSharper disable UnusedMember.Global

namespace Happy_Reader
{
    public static class StaticMethods
    {
        public delegate void NotificationEventHandler(object sender, string message, string title = null);

        private const string ConfigFolder = "Config\\";
        public const string ProxiesJson = ConfigFolder + "proxies.json";
        public static HappyReaderDatabase Data { get; } = new HappyReaderDatabase();

        public static GuiSettings GSettings { get; set; } = StaticHelpers.GSettings;

        static StaticMethods()
        {
            try
            {
                Directory.CreateDirectory(ConfigFolder);
            }
            catch (Exception ex)
            {
                StaticHelpers.LogToFile(ex);

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogReplace(this StringBuilder sb, string input, string output, long id)
        {
#if LOGVERBOSE
            var sbOriginal = sb.ToString();
            sb.Replace(input, output);
            var sbReplaced = sb.ToString();
            if (sbOriginal != sbReplaced)
            {
                Debug.WriteLine($"Replace happened - id {id}: '{input}' > '{output}'");
            }
#else
            sb.Replace(input, output);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogReplaceRegex(this StringBuilder sb, string input, string output, long id)
        {
            var rgx = new Regex(input);
#if LOGVERBOSE
            var sbOriginal = sb.ToString();
            var sbReplaced = rgx.Replace(sbOriginal, output);
            if (sbOriginal != sbReplaced)
            {
                Debug.WriteLine($"Replace happened - id {id}: '{input}' > '{output}'");
            }
#else
            var sbReplaced = rgx.Replace(sb.ToString(), output);
#endif
            sb.Clear();
            sb.Append(sbReplaced);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogToConsole(string message)
        {
            Console.WriteLine(message);
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
                UseShellExecute = true,
                WorkingDirectory = exeParentFolder
            };
            return Process.Start(pi);
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
            foreach (var translation in Translator.GetCache())
            {
                if (Data.CachedTranslations.Local.Contains(translation)) continue;
                Data.CachedTranslations.Add(translation);
            }
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
