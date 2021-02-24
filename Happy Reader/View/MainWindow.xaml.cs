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
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Tabs;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
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
			EventHandler open = (_, _) => Show();
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
			var noTranslation = commandLineArgs.Contains("-nt");
			var logVerbose = commandLineArgs.Contains("-lv");
			await ViewModel.Initialize(watch, UserGamesTabItem.GroupByAdded, !noEntries, noTranslation, logVerbose);
		}

		public void ShowLogNotification([NotNull] Log message)
		{
			Happy_Apps_Core.StaticHelpers.Logger.ToDebug($"Notification - {message.Kind} - {message}");
			NotificationWindow.Launch(message);
		}

		public void ShowNotification(object sender, [NotNull] string message, string title = "Notification")
		{
			Happy_Apps_Core.StaticHelpers.Logger.ToDebug($"Notification - {title} - {message}");
			NotificationWindow.Launch(title, message);
		}

		public void TabMiddleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Middle) return;
			var tabItem = (TabItem)sender;
			tabItem.Template = null;
			MainTabControl.Items.Remove(tabItem);
		}

		public void CreateAddEntriesTab(ICollection<Entry> entries)
		{
			var vnEntries = ViewModel.UserGamesViewModel.UserGameItems
				.Select(i => i.UserGame.VN)
				.Concat(entries.Select(e => e.Game))
				.Distinct()
				.Where(i => i != null)
				.ToArray();
			var tabItem = new TabItem
			{
				Header = "Add Entries",
				Name = nameof(AddEntriesTab),
				Content = new AddEntriesTab(ViewModel, vnEntries, entries)
			};
			AddTabItem(tabItem, null);
		}

		public void OpenVNPanel(ListedVN vn)
		{
			var userGame = StaticMethods.Data.UserGames.FirstOrDefault(ug => ug.VNID == vn.VNID);
			if (userGame != null)
			{
				var userGameTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == userGame);
				if (userGameTab != null) MainTabControl.Items.Remove(userGameTab);
			}
			var vnTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.DataContext == vn);
			if (vnTab != null)
			{
				MainTabControl.SelectedItem = vnTab;
				vnTab.Focus();
				return;
			}
			var tabItem = new TabItem
			{
				Name = nameof(VNTab),
				Content = new VNTab(vn, userGame),
				DataContext = (object)userGame ?? vn
			};
			var headerBinding = new Binding(userGame != null ? nameof(UserGame.DisplayName) : nameof(ListedVN.Title))
			{ Source = tabItem.DataContext };
			AddTabItem(tabItem, headerBinding);
		}

		public void OpenUserGamePanel(UserGame userGame, ListedVN priorVN)
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
			AddTabItem(tabItem, headerBinding);
		}

		private void AddTabItem(HeaderedContentControl tabItem, BindingBase headerBinding)
		{
			var headerTextBlock = new TextBlock
			{
				Text = (string)tabItem.Header,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			if (headerBinding != null) headerTextBlock.SetBinding(TextBlock.TextProperty, headerBinding);
			var header = new Grid
			{
				Background = tabItem.Content switch
				{
					VNTab => Brushes.HotPink,
					UserGameTab => Brushes.DarkKhaki,
					_ => Brushes.IndianRed
				},
				Width = 100,
				Height = 50,
				VerticalAlignment = VerticalAlignment.Stretch,
				HorizontalAlignment = HorizontalAlignment.Stretch
			};
			header.Children.Add(headerTextBlock);
			tabItem.MouseDown += TabMiddleClick;
			tabItem.Header = header;
			MainTabControl.Items.Add(tabItem);
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
			if (_finalizing) return;
			_finalizing = true;
			Hide();
			_trayIcon.Visible = false;
			ViewModel.ExitProcedures(this, null);
			_finalized = true;
			Close();
		}

		public void SelectTab(Type viewModelType)
		{
			var tab = MainTabControl.Items.OfType<TabItem>().FirstOrDefault(t => ((FrameworkElement) t.Content).DataContext.GetType() == viewModelType);
			MainTabControl.SelectedItem = tab ?? throw new ArgumentNullException(nameof(tab), $"Did not find tab with ViewModel of type {viewModelType}");
		}

	}
}