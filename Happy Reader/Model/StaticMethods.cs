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
using Happy_Reader.Database;
using Happy_Reader.Properties;
using Newtonsoft.Json;

namespace Happy_Reader
{
    public static class StaticMethods
    {
        public delegate void NotificationEventHandler(object sender, string message, string title = null);

        private const string ConfigFolder = "Config\\";
        private const string BannedProcessesJson = ConfigFolder + "bannedprocesses.json";
        public static SessionSettings Session { get; private set; }
        private static readonly List<string> BannedProcesses;
        public static HappyReaderDatabase Data { get; } = new HappyReaderDatabase();

        static StaticMethods()
        {
            Session = new SessionSettings();
            List<string> result = null;
            try
            {
                Directory.CreateDirectory(ConfigFolder);
                result = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(BannedProcessesJson));
            }
            catch (Exception)
            {
                //TODO log error

            }
            finally
            {
                BannedProcesses = result ?? new List<string>();
            }
        }

        public static void ResetSession()
        {
            Session = new SessionSettings();
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

        public static void BanProcess(string processName)
        {
            if (processName == null || BannedProcesses.Contains(processName)) return;
            BannedProcesses.Add(processName);
            SaveBannedProcesses();

        }

        private static void SaveBannedProcesses()
        {
            File.WriteAllText(BannedProcessesJson, JsonConvert.SerializeObject(BannedProcesses, Formatting.Indented));
        }

        public static bool ProcessIsBanned(string processName) => BannedProcesses.Contains(processName);

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
        {
            return MessageBox.Show(message, "Happy Reader - Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }
    }
}
