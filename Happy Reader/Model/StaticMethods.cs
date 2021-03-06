﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Happy_Apps_Core;
using Happy_Reader.Database;
using System.Linq.Expressions;
using System.Management;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Apps_Core.Database;
using Happy_Reader.View;
using Happy_Reader.ViewModel;
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
		public static readonly string AllSettingsJson = Path.Combine(StaticHelpers.StoredDataFolder, "HR_Settings.json");
		public static readonly string SavedDataJson = Path.Combine(StaticHelpers.StoredDataFolder, "HR_SavedData.json");
		public static readonly string AllFiltersJson = Path.Combine(StaticHelpers.StoredDataFolder, "HR_Filters.json");
		public static readonly JsonSerializerSettings SerialiserSettings = new() { TypeNameHandling = TypeNameHandling.Objects };
		public static readonly Rectangle OutputWindowStartPosition = new(20, 20, 400, 200);
		private static SettingsViewModel _settings;
		public static FiltersData AllFilters { get; set; }
		public static HappyReaderDatabase Data { get; } = new();
		public static Func<bool> ShowNSFWImages { get; set; } = () => true;
		public static MainWindow MainWindow => (MainWindow)Application.Current.MainWindow;
		public static SettingsViewModel Settings
		{
			get
			{
				if (_settings == null) throw new ArgumentNullException(nameof(_settings), $"{nameof(SettingsViewModel)} must be initialized first.");
				return _settings;
			}
			set
			{
				if (_settings != null) throw new InvalidOperationException($"{nameof(SettingsViewModel)} must only be set once.");
				_settings = value;
			}
		}

		public static string GetLocalizedTime(this DateTime dateTime)
		{
			var culture = Settings.GuiSettings.CultureInfo;
			bool isAmPm = culture.DateTimeFormat.AMDesignator != string.Empty;
			return dateTime.ToString(isAmPm ? "hh:mm tt" : "HH:mm", culture);
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
			return result.Length == 0 ? new ComboBoxItem[0] : result.Cast<Enum>().Where(t => !AttributeIsDefined(t, typeof(NotMappedAttribute))).Select(x => new ComboBoxItem
			{
				Content = x.GetDescription(),
				Tag = x,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				VerticalContentAlignment = VerticalAlignment.Center
			}).ToArray();
		}

		public static bool AttributeIsDefined(object value, Type attributeType)
		{
			var field = value.GetType().GetField(value.ToString());
			return Attribute.IsDefined(field, attributeType);
		}

		//todo make this an externally loaded list
		public static HashSet<string> ExcludedNamesForVNResolve = new(StringComparer.OrdinalIgnoreCase)
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

		public static bool CtrlKeyIsHeld() => DispatchIfRequired(() => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));

		public static void DispatchIfRequired(Action action, TimeSpan timeout)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			if (Application.Current.Dispatcher.CheckAccess()) action();
			else Application.Current.Dispatcher.Invoke(action, timeout);
		}

		private static T DispatchIfRequired<T>(Func<T> action)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			return Application.Current.Dispatcher.CheckAccess() ? action() : Application.Current.Dispatcher.Invoke(action);
		}
	}

	public class FiltersData : SettingsJsonFile
	{
		public ObservableCollection<CustomFilter> VnFilters { get; set; } = new();
		public ObservableCollection<CustomFilter> CharacterFilters { get; set; } = new();
		public CustomFilter VnPermanentFilter { get; set; } = new();
		public CustomFilter CharacterPermanentFilter { get; set; } = new();
	}
}
