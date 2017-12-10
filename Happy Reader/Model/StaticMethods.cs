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

namespace Happy_Reader
{
    public static class StaticMethods
    {
        public delegate void NotificationEventHandler(object sender, string message, string title = null);

        private const string ConfigFolder = "Config\\";
        public const string ProxiesJson = ConfigFolder + "proxies.json";
        public static HappyReaderDatabase Data { get; } = new HappyReaderDatabase();

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
                return default(TValue);
            }
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            return dict.TryGetValue(key, out TValue result) ? result : defaultValue;
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
            return rct;
        }
    }
}
