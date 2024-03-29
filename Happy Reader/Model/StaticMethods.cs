﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Happy_Apps_Core;
using Happy_Reader.Database;
using System.Management;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Happy_Apps_Core.Database;
using Happy_Reader.TranslationEngine;
using Happy_Reader.View;
using Happy_Reader.View.Tabs;
using Happy_Reader.ViewModel;
using Newtonsoft.Json;
using FontFamily = System.Windows.Media.FontFamily;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

// ReSharper disable UnusedMember.Global

namespace Happy_Reader
{
	public static class StaticMethods
	{
		public delegate void NotificationEventHandler(object sender, string message, string title, bool showInWindow);

        public static readonly string ReaderDatabaseFile = Path.Combine(StaticHelpers.StoredDataFolder, "Happy-Reader.sqlite");
        public static readonly string DeinflectionsDatabaseFile = Path.Combine(StaticHelpers.StoredDataFolder, "Happy-Reader-Deinflections.sqlite");
        public static readonly string ProxiesJson = Path.Combine(StaticHelpers.ProgramDataFolder, "Default Files\\proxies.json");
		public static readonly string SavedDataJson = Path.Combine(StaticHelpers.StoredDataFolder, "HR_SavedData.json");
		public static readonly string AllFiltersJson = Path.Combine(StaticHelpers.StoredDataFolder, "HR_Filters.json");
		public static readonly string UserGameIconsFolder = Path.Combine(StaticHelpers.StoredDataFolder, "Usergame_Icons\\");
		private static readonly string FlagsFolder = Path.Combine(StaticHelpers.ProgramDataFolder, "Flags\\");
		public static readonly JsonSerializerSettings SerialiserSettings = new() { TypeNameHandling = TypeNameHandling.Objects };
		public static readonly NativeMethods.RECT OutputWindowStartPosition = new() { Left = 20, Top = 20, Right = 420, Bottom = 220 };
		private static readonly Dictionary<string, ImageSource> FlagExistsDictionary = new();
		private static SettingsViewModel _settings;
		public static FiltersData AllFilters { get; set; }
		public static HappyReaderDatabase Data { get; set; }
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

		static StaticMethods()
		{
			var fonts = 
				//select all font families
				Fonts.SystemFontFamilies
					//for each font family
					.SelectMany(ff =>
						//select unique font names
						ff.FamilyNames.GroupBy(fn => fn.Value).Select(fng => fng.First())
							//create map of font name to font family (for this font family)
							.Select(f => new KeyValuePair<string, FontFamily>(f.Value, ff))).ToArray();
				var dict = new Dictionary<string, FontFamily>();
				foreach (var pair in fonts)
				{
					if(dict.ContainsKey(pair.Key)) StaticHelpers.Logger.ToFile($"Duplicate font family found: {pair.Key}");
					dict[pair.Key] = pair.Value;
				}
				FontsInstalled = new ReadOnlyDictionary<string, FontFamily>(dict);
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

		public static ListedVN ResolveVNForFile(string file)
		{
			var filename = Path.GetFileNameWithoutExtension(file);
			ListedVN[] fileResults = StaticHelpers.LocalDatabase.VisualNovels.Where(VisualNovelDatabase.SearchForVN(filename)).ToArray();
			ListedVN vn = null;
			if (fileResults.Length == 1 && !Settings.GuiSettings.ExcludedNamesForVNResolve.Contains(filename)) vn = fileResults.First();
			else
			{
				var parent = Directory.GetParent(file);
				//if parent is not null, and if parent's parent  is not null (if parent's parent is null, then it is the root location.
				while (parent?.Parent != null)
				{
					if (Settings.GuiSettings.ExcludedNamesForVNResolve.Contains(parent.Name))
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

		public static readonly IReadOnlyDictionary<string, FontFamily> FontsInstalled;
		
		public static string GetProcessFileName(Process process)
		{
			string processFileName;
			if (process.Is64BitProcess())
			{
				var searcher = new ManagementObjectSearcher("root\\CIMV2", $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {process.Id}");
				processFileName = searcher.Get().Cast<ManagementObject>().FirstOrDefault()?["ExecutablePath"]?.ToString() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(processFileName)) throw new InvalidOperationException("Did not find executable path for process");
			}
			else
			{
				Debug.Assert(process.MainModule != null, "gameProcess.MainModule != null");
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

		public static T DispatchIfRequired<T>(Func<T> action)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			return Application.Current.Dispatcher.CheckAccess() ? action() : Application.Current.Dispatcher.Invoke(action);
		}
		
        public static Grid GetTabHeader(string text, BindingBase headerBinding, object content, HashSet<SavedData.SavedTab> savedTabs)
        {
            var headerTextBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            if (headerBinding != null) headerTextBlock.SetBinding(TextBlock.TextProperty, headerBinding);
            var header = new Grid
            {
                Width = 100,
                Height = 50,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = GetTabHeaderBackgroundAndSaveTab(content, savedTabs)
            };
            header.Children.Add(headerTextBlock);
            return header;
        }
		
		/// <summary>
		/// Adds tab to <see cref="savedTabs"/> if it is not null.
		/// </summary>
        public static Brush GetTabHeaderBackgroundAndSaveTab(object content, HashSet<SavedData.SavedTab> savedTabs)
        {
            switch (content)
            {
				case VNTab vnTab:
                    savedTabs?.Add(new SavedData.SavedTab(vnTab.ViewModel.VNID, nameof(VNTab)));
                    return Theme.VNTabBackground;
                case UserGameTab gameTab:
                    savedTabs?.Add(new SavedData.SavedTab(gameTab.ViewModel.Id, nameof(UserGameTab)));
                    return Theme.UserGameTabBackground;
                case ProducerTab producerTab:
                    savedTabs?.Add(new SavedData.SavedTab(producerTab.ViewModel.ID, nameof(ProducerTab)));
                    return Theme.ProducerTabBackground;
                default:
                    return Theme.TypeNotFoundTabBackground;
            }
        }

        public static ToolTip CreateMouseoverTooltip(UIElement placementTarget, PlacementMode placementMode)
		{
			var tooltip = new ToolTip()
			{
				Placement = placementMode,
				Background = Theme.MouseoverTooltipBackground,
				Foreground = Theme.MouseoverTooltipForeground 
			};
			if (placementTarget != null) tooltip.PlacementTarget = placementTarget;
			return tooltip;
		}

		public static void UpdateTooltip(ToolTip mouseoverTip, string text)
		{
			if (text.Length < 1 || Translator.LatinOnlyRegex.IsMatch(text)) return;
			if (!Translator.Instance.OfflineDictionary.SearchOuter(text, out var result)) return;
			if (result.Equals(mouseoverTip.Content)) return;
			mouseoverTip.Content = result;
			if (!mouseoverTip.IsOpen) mouseoverTip.IsOpen = true;
		}

		public static ImageSource GetFlag(VNLanguages languages, int order, out LangRelease release)
		{
			int counter = 0;
			foreach (var lang in languages.All)
			{
				var filePath = $"{FlagsFolder}{lang.Lang}.png";
				if (!FlagExistsDictionary.TryGetValue(filePath, out var exists))
				{
					exists = FlagExistsDictionary[filePath] = File.Exists(Path.GetFullPath(filePath)) ? new BitmapImage(new Uri(Path.GetFullPath(filePath))) : null;
				}
				if (exists != null) counter++;
				if (counter == order)
				{
					release = lang;
					return exists;
				}
			}
			release = null;
			return null;
		}

		public static ImageSource GetFlag(string language)
		{
			if (language == null) return null;
			var languageFile = $"{FlagsFolder}{language}.png";
			if (!FlagExistsDictionary.TryGetValue(languageFile, out var exists))
			{
				exists = FlagExistsDictionary[languageFile] = File.Exists(Path.GetFullPath(languageFile)) ? new BitmapImage(new Uri(Path.GetFullPath(languageFile))) : null;
			}
			return exists;
		}

        public static void GetDateFromVisualNovel(ListedVN vn, out DateTime releaseDate, out string releaseDateString)
        {
            var secondary = MainWindow.ViewModel.DatabaseViewModel.OrderingType == TitleOrderingType.SecondaryDate;
            releaseDate = secondary ? vn.ReleaseDateSecondary : vn.ReleaseDate;
            releaseDateString = secondary ? releaseDate.ToString("yyyy-MM-dd") : vn.ReleaseDateString;
        }

        public static int FindIndex<T>(this ObservableCollection<T> source, Func<T, bool> predicate)
        {
            var index = -1;
            foreach (var item in source)
            {
                index++;
				if (predicate(item)) return index;
            }
            return -1;
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
