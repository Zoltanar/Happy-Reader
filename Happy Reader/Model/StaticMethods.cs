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
using System.Windows.Controls;
using System.Windows.Media;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;
using FontFamily = System.Windows.Media.FontFamily;
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
		public static readonly string CustomCharacterFiltersJson = Path.Combine(StaticHelpers.StoredDataFolder, "CustomCharacterFilters.json");
		public static readonly string PermanentCharacterFilterJson = Path.Combine(StaticHelpers.StoredDataFolder, "PermanentCharacterFilter.json");
		public static readonly string GuiSettingsJson = Path.Combine(StaticHelpers.StoredDataFolder, "guisettings.json");
		public static readonly string TranslatorSettingsJson = Path.Combine(StaticHelpers.StoredDataFolder, "translatorsettings.json");
		public static readonly GuiSettings GuiSettings;
		public static readonly TranslatorSettings TranslatorSettings;
		public static HappyReaderDatabase Data { get; } = new ();
		public static Func<bool> ShowNSFWImages { get; set; } = () => true;
		public static JsonSerializerSettings SerialiserSettings { get; } = new(){TypeNameHandling = TypeNameHandling.Objects};

		static StaticMethods()
		{
			GuiSettings = SettingsJsonFile.Load<GuiSettings>(GuiSettingsJson);
			TranslatorSettings = SettingsJsonFile.Load<TranslatorSettings>(TranslatorSettingsJson);
		}

		public static string GetLocalizedTime(this DateTime dateTime)
		{
			bool isAmPm = GuiSettings.CultureInfo.DateTimeFormat.AMDesignator != string.Empty;
			return dateTime.ToString(isAmPm ? "hh:mm tt" : "HH:mm", GuiSettings.CultureInfo);
		}
		
		public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
		{
			while (true)
			{
				//get parent item
				var parentObject = VisualTreeHelper.GetParent(child);
				switch (parentObject)
				{
					//we've reached the end of the tree
					case null:
						return null;
					//check if the parent matches the type we're looking for
					case T parent:
						return parent;
					default:
						child = parentObject;
						break;
				}
			}
		}

		public static Collection<T> GetVisualChildren<T>(DependencyObject current) where T : DependencyObject
		{
			if (current == null) return null;
			var children = new Collection<T>();
			GetVisualChildren(current, children);
			return children;
		}

		private static void GetVisualChildren<T>(DependencyObject current, ICollection<T> children) where T : DependencyObject
		{
			if (current == null) return;
			if (current.GetType() == typeof(T)) children.Add((T)current);
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
			{
				GetVisualChildren(VisualTreeHelper.GetChild(current, i), children);
			}
		}

		public static T GetDescendantByType<T>(this Visual element) where T : Visual
		{
			switch (element)
			{
				case null: return null;
				case T typedElement: return typedElement;
			}
			Visual foundElement = null;
			if (element is FrameworkElement frameworkElement) frameworkElement.ApplyTemplate();
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
			{
				var visual = VisualTreeHelper.GetChild(element, i) as Visual;
				foundElement = GetDescendantByType<T>(visual);
				if (foundElement != null) break;
			}
			return (T)foundElement;
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
		
		/// <summary>
		/// Truncates strings if they are longer than 'maxChars' (minimum is 4 characters).
		/// </summary>
		/// <param name="maxChars">The maximum number of characters</param>
		/// <returns>Truncated string</returns>
		private static Expression<Func<string, string>> TruncateStringExpression(int maxChars)
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

		public static readonly string[] ResolveNamesToSkip = { };

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

		public static readonly Dictionary<string, FontFamily> FontsInstalled =
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
				processFileName = searcher.Get().Cast<ManagementObject>().FirstOrDefault()?["ExecutablePath"].ToString() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(processFileName)) throw new InvalidOperationException("Did not find executable path for process");
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
