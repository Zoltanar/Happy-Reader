using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Tabs;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;

namespace Happy_Reader.View
{
	public partial class MainWindow
	{
		private bool _finalizing;
		private bool _finalized;
		private NotifyIcon _trayIcon;
		private readonly SavedData _savedData = new();

		public MainWindowViewModel ViewModel { get; }

		public MainWindow()
		{
			InitializeComponent();
			ViewModel = new MainWindowViewModel();
			DataContext = ViewModel;
			CreateNotifyIcon();
			ViewModel.NotificationEvent += ShowNotification;
			Log.NotificationEvent += ShowLogNotification;
		}

		private void CreateNotifyIcon()
		{
			var contextMenu = new ContextMenuStrip();
			EventHandler open = (_, _) =>
			{
				Show();
				Activate();
			};
			contextMenu.Items.Add(new ToolStripMenuItem("Open", null, open));
			contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, Exit));
			// ReSharper disable once PossibleNullReferenceException
			Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/logo-hr.ico")).Stream;
			_trayIcon = new NotifyIcon
			{
				Icon = new System.Drawing.Icon(iconStream),
				ContextMenuStrip = contextMenu,
				Visible = true
			};
			_trayIcon.DoubleClick += open;
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this)) return;
			Stopwatch watch = Stopwatch.StartNew();
			ViewModel.ClipboardManager = new ClipboardManager(this);
			ViewModel.CaptureClipboardSettingChanged(ViewModel.SettingsViewModel.TranslatorSettings.CaptureClipboard);
			var commandLineArgs = Environment.GetCommandLineArgs();
			var noEntries = commandLineArgs.Contains("-ne");
			var logVerbose = commandLineArgs.Contains("-lv");
			await ViewModel.Initialize(watch, UserGamesTabItem.GroupByAdded, !noEntries, logVerbose);
			LoadSavedData();
		}

		private void LoadSavedData()
		{
			if (!File.Exists(StaticMethods.SavedDataJson)) return;
			SavedData savedData;
			try
			{
				savedData = JsonConvert.DeserializeObject<SavedData>(File.ReadAllText(StaticMethods.SavedDataJson));
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				return;
			}
			if (savedData == null) return;
			foreach (var savedTab in savedData.Tabs)
			{
				switch (savedTab.TypeName)
				{
					case nameof(VNTab):
						{
							var vn = StaticHelpers.LocalDatabase.VisualNovels[(int)savedTab.Id];
							if (vn != null) OpenVNPanel(vn, false);
							break;
						}
					case nameof(UserGameTab):
						{
							var userGame = StaticMethods.Data.UserGames[savedTab.Id];
							if (userGame != null) OpenUserGamePanel(userGame, null, false);
							break;
						}
				}
			}
		}

		public void ShowLogNotification([NotNull] Log message)
		{
			StaticHelpers.Logger.ToDebug($"Notification - {message.Kind} - {message}");
			NotificationWindow.Launch(message);
		}

		public void ShowNotification(object sender, [NotNull] string message, string title = "Notification")
		{
			StaticHelpers.Logger.ToDebug($"Notification - {title} - {message}");
			NotificationWindow.Launch(title, message);
		}

		public void TabMiddleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Middle) return;
			var tabItem = ((DependencyObject) sender).FindParent<TabItem>();
			tabItem.Template = null;
			var content = tabItem.Content;
			switch (content)
			{
				case VNTab vnTab:
					_savedData.Tabs.RemoveWhere(st => st.TypeName == nameof(VNTab) && st.Id == vnTab.ViewModel.VNID);
					break;
				case UserGameTab gameTab:
					_savedData.Tabs.RemoveWhere(st => st.TypeName == nameof(UserGameTab) && st.Id == gameTab.ViewModel.Id);
					break;
			}
			MainTabControl.Items.Remove(tabItem);
		}

		public void CreateAddEntriesTab(IEnumerable<Entry> entries)
		{
			var newEntries = entries.Except(StaticMethods.Data.Entries, Entry.ClashComparer).ToList();
			if (newEntries.Count == 0)
			{
				ViewModel.NotificationEvent(this, "No new names to import.", "Import Names");
				return;
			}
			var vnEntries = ViewModel.UserGamesViewModel.UserGameItems
				.Select(i => i.UserGame.VN)
				.Concat(newEntries.Select(e => e.Game))
				.Distinct()
				.Where(i => i != null)
				.ToArray();
			var tabItem = new TabItem
			{
				Header = "Add Entries",
				Name = nameof(AddEntriesTab),
				Content = new AddEntriesTab(ViewModel, vnEntries, newEntries)
			};
			AddTabItem(tabItem, null, true);
		}

		public void OpenVNPanel(ListedVN vn, bool select = true)
		{
			var userGamesForVn = StaticMethods.Data.UserGames.Where(ug => ug.VNID == vn.VNID).ToList();
			//remove existing user game tabs if any
			foreach (var userGame in userGamesForVn)
			{
				var userGameTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == userGame);
				if (userGameTab != null) MainTabControl.Items.Remove(userGameTab);
			}
			var vnTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Content is VNTab vTab && vTab.ViewModel == vn);
			if (vnTab != null)
			{
				if(((VNTab)vnTab.Content).UserGames.Intersect(userGamesForVn).Count() == userGamesForVn.Count)
				{
					//if tab already exists with same userGames
					if (!select) return;
					MainTabControl.SelectedItem = vnTab;
					vnTab.Focus();
					return;
				}
				//else, remove tab and re-create with new user games
				MainTabControl.Items.Remove(vnTab);
			}

			var hasSingleUserGame = userGamesForVn.Count == 1;
			var tabItem = new TabItem
			{
				Name = nameof(VNTab),
				Content = new VNTab(vn, userGamesForVn),
				DataContext = hasSingleUserGame ? userGamesForVn.First() : vn
			};
			var headerBinding = new Binding(hasSingleUserGame ? nameof(UserGame.DisplayName) : nameof(ListedVN.Title))
			{ Source = tabItem.DataContext };
			AddTabItem(tabItem, headerBinding, select);
		}

		public void OpenUserGamePanel(UserGame userGame, ListedVN priorVN, bool select = true)
		{
			if (priorVN != null)
			{
				var vnTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == priorVN);
				if (vnTab != null) MainTabControl.Items.Remove(vnTab);
			}
			var userGameTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == userGame);
			if (userGameTab != null)
			{
				MainTabControl.SelectedItem = userGameTab;
				userGameTab.Focus();
				return;
			}
			var tabItem = new TabItem
			{
				Name = nameof(UserGameTab),
				Content = new UserGameTab(userGame, false),
				DataContext = userGame
			};
			var headerBinding = new Binding(nameof(UserGame.DisplayName))
			{
				Source = tabItem.DataContext
			};
			AddTabItem(tabItem, headerBinding, select);
		}

		public static Grid GetTabHeader(string text, BindingBase headerBinding, object content, HashSet<SavedData.SavedTab> savedTabs)
		{
			var headerTextBlock = new TextBlock
			{
				Text = text,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			if (headerBinding != null) headerTextBlock.SetBinding(TextBlock.TextProperty, headerBinding); var header = new Grid
			{
				Width = 100,
				Height = 50,
				VerticalAlignment = VerticalAlignment.Stretch,
				HorizontalAlignment = HorizontalAlignment.Stretch
			}; 
			switch (content)
			{
				case VNTab vnTab:
					savedTabs.Add(new SavedData.SavedTab(vnTab.ViewModel.VNID, nameof(VNTab)));
					header.Background = Brushes.HotPink;
					break;
				case UserGameTab gameTab:
					savedTabs.Add(new SavedData.SavedTab(gameTab.ViewModel.Id, nameof(UserGameTab)));
					header.Background = Theme.UserGameTabBackground;
					break;
				default:
					header.Background = Brushes.IndianRed;
					break;
			}
			header.Children.Add(headerTextBlock);
			return header;
		}

		private void AddTabItem(HeaderedContentControl tabItem, BindingBase headerBinding, bool select)
		{
			var header = GetTabHeader((string)tabItem.Header, headerBinding, tabItem.Content, _savedData.Tabs);
			header.MouseDown += TabMiddleClick;
			tabItem.Header = header;
			MainTabControl.Items.Add(tabItem);
			if (!select) return;
			MainTabControl.SelectedItem = tabItem;
			tabItem.Focus();
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
		{
			if (_finalized) return;
			e.Cancel = true;
			Hide();
		}

		private void Exit(object sender, EventArgs e)
		{
			if (_finalizing || _finalized) return;
			_finalizing = true;
			Hide();
			_trayIcon.Visible = false;
			ViewModel.ExitProcedures(this, null);
			SaveTabsToFile();
			_finalized = true;
			Close();
		}

		private void SaveTabsToFile()
		{
			try
			{
				File.WriteAllText(StaticMethods.SavedDataJson, JsonConvert.SerializeObject(_savedData, Formatting.Indented));
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
		}

		public void SelectTab(Type viewModelType)
		{
			var tab = MainTabControl.Items.OfType<TabItem>().FirstOrDefault(t => ((FrameworkElement)t.Content).DataContext.GetType() == viewModelType);
			MainTabControl.SelectedItem = tab ?? throw new ArgumentNullException(nameof(tab), $"Did not find tab with ViewModel of type {viewModelType}");
		}
	}
}