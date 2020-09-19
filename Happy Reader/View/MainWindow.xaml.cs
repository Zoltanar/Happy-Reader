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
using System.Windows.Threading;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Tabs;
using Happy_Reader.View.Tiles;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;

namespace Happy_Reader.View
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindowViewModel ViewModel { get; }

		public MainWindow()
		{
			InitializeComponent();
			ViewModel = new MainWindowViewModel(this);
			DataContext = ViewModel;
			CreateNotifyIcon();
			ViewModel.NotificationEvent += ShowNotification;
			Log.NotificationEvent += ShowLogNotification;
		}

		private void CreateNotifyIcon()
		{
			var contextMenu = new ContextMenuStrip();
			EventHandler open = (o, e) => Show();
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
			var commandLineArgs = Environment.GetCommandLineArgs();
			var noHook = commandLineArgs.Contains("-nh");
			var noEntries = commandLineArgs.Contains("-ne");
			var noTranslation = commandLineArgs.Contains("-nt");
			if (noHook) IthTabItem.Visibility = Visibility.Collapsed;
			if (noEntries)
			{
				EntriesTabItem.Visibility = Visibility.Collapsed;
				TestTabItem.Visibility = Visibility.Collapsed;
			}
			await ViewModel.Initialize(watch, GroupByAdded, !noHook, !noEntries, noTranslation);
		}

		private void AddEntry_Click(object sender, RoutedEventArgs e) => CreateAddEntryTab(null, null, false);

		private void DropFileOnGamesTab(object sender, DragEventArgs e)
		{
			string file = (e.Data.GetData(DataFormats.FileDrop) as string[])?.First();
			if (string.IsNullOrWhiteSpace(file))
			{
				ViewModel.StatusText = "Dragged item was not a file.";
				return;
			}
			var ext = Path.GetExtension(file);
			if (!ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
			{
				ViewModel.StatusText = "Dragged file isn't an executable.";
				return;
			}
			var titledImage = ViewModel.AddGameFile(file);
			if (titledImage == null) return;
			((IList<UserGameTile>)GameFiles.ItemsSource).Add(titledImage);
			titledImage.ViewDetails(this, null);
		}

		private void GameFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{/*
			var item = GameFiles.SelectedItem as UserGameTile;
			var userGame = (UserGame)item?.DataContext;
			if (userGame == null) return;
			ViewModel.HookUserGame(userGame, null, false);*/
		}

		public void ShowLogNotification([NotNull] Log message)
		{
			Console.WriteLine($"Notification - {message.Kind} - {message}");
			NotificationWindow.Launch(message);
		}

		public void ShowNotification(object sender, [NotNull] string message, string title = "Notification")
		{
			Console.WriteLine($"Notification - {title} - {message}");
			NotificationWindow.Launch(title, message);
		}

		public void TabMiddleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Middle) return;
			var tabItem = (TabItem)sender;
			tabItem.Template = null;
			MainTabControl.Items.Remove(tabItem);
		}

		public void CreateAddEntryTab(string input, string output, bool seriesSpecific)
		{
			var tabItem = new TabItem
			{
				Header = "Add Entry",
				Name = nameof(AddEntryControl),
				Content = new AddEntryControl(ViewModel, input, output, seriesSpecific)
			};
			AddTabItem(tabItem);
		}

		public void OpenVNPanel(ListedVN vn, bool openOnUserGame)
		{
			var userGame = StaticMethods.Data.UserGames.FirstOrDefault(ug => ug.VNID == vn.VNID);
			if (userGame != null)
			{
				var userGameTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == userGame);
				if (userGameTab != null) MainTabControl.Items.Remove(userGameTab);
			}
			var vnTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == vn);
			if (vnTab != null)
			{
				MainTabControl.SelectedItem = vnTab;
				vnTab.Focus();
				return;
			}
			var tabItem = new TabItem
			{
				Header = userGame?.DisplayName ?? StaticHelpers.TruncateString(vn.Title, 30),
				Name = nameof(VNTab),
				Content = new VNTab(vn, userGame, openOnUserGame),
				Tag = vn
			};
			AddTabItem(tabItem);
		}

		public void OpenUserGamePanel(UserGame userGame, ListedVN priorVN)
		{
			if (priorVN != null)
			{
				var vnTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == priorVN);
				if (vnTab != null) MainTabControl.Items.Remove(vnTab);
			}
			var userGameTab = MainTabControl.Items.Cast<TabItem>().FirstOrDefault(t => t.Tag == userGame);
			if (userGameTab != null)
			{
				MainTabControl.SelectedItem = userGameTab;
				userGameTab.Focus();
				return;
			}
			var tabItem = new TabItem
			{
				Header = userGame.DisplayName,
				Name = nameof(UserGameTab),
				Content = new UserGameTab(userGame, false),
				Tag = userGame
			};
			AddTabItem(tabItem);
		}

		private void AddTabItem(TabItem tabItem)
		{
			var header = new Grid();
			header.Children.Add(new TextBlock
			{
				Text = (string)tabItem.Header,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center
			});
			tabItem.MouseDown += TabMiddleClick;
			tabItem.Header = header;
			MainTabControl.Items.Add(tabItem);
			MainTabControl.SelectedItem = tabItem;
			tabItem.Focus();
		}

		private void GroupByProducer(object sender, RoutedEventArgs e)
		{
			var groupProperty = $"{nameof(UserGame)}.{nameof(UserGame.VN)}.{nameof(ListedVN.Producer)}.{nameof(ListedProducer.Name)}";
			GroupUserGameItems(
				new PropertyGroupDescription(groupProperty),
				true, new SortDescription(groupProperty, ListSortDirection.Descending));
		}

		private void GroupByMonth(object sender, RoutedEventArgs e)
		{
			GroupUserGameItems(
				new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.MonthGroupingString)}"),
				false, new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.MonthGrouping)}", ListSortDirection.Descending));
		}

		private void GroupByName(object sender, RoutedEventArgs e)
		{
			GroupUserGameItems(
				new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayNameGroup)}"),
				false, new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayName)}", ListSortDirection.Ascending));
		}

		private void GroupByLastPlayed(object sender, RoutedEventArgs e)
		{
			var groupName = $@"{nameof(UserGame)}.{nameof(UserGame.LastPlayedDate)}";
			var groupDescription = new PropertyGroupDescription(groupName, new LastPlayedConverter());
			GroupUserGameItems(
				groupDescription,
				false, new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.LastPlayedDate)}", ListSortDirection.Descending));
			ToggleLastGroups(groupName, 2, false);
		}

		private void GroupByTimePlayed(object sender, RoutedEventArgs e)
		{
			var groupName = $@"{nameof(UserGame)}.{nameof(UserGame.TimeOpen)}";
			var groupDescription = new PropertyGroupDescription(groupName, new TimeOpenConverter());
			GroupUserGameItems(
				groupDescription,
				false, new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayName)}", ListSortDirection.Descending));
			ToggleLastGroups(groupName, 2, true);
		}

		private void GroupByAdded(object sender, RoutedEventArgs e)
		{
			GroupUserGameItems(
				null,
				false, new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.Id)}", ListSortDirection.Descending));
		}

		private void GroupByTag(object sender, RoutedEventArgs e)
		{
			var groupDescription = new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.Tag)}", new TagConverter());
			GroupUserGameItems(
				groupDescription,
				false,
				new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.TagSort)}", ListSortDirection.Descending),
			new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayName)}", ListSortDirection.Ascending));
		}

		private void GroupUserGameItems(
			GroupDescription groupDescription,
			bool setHeaderStringFormatNull,
			params SortDescription[] sortDescriptions)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ViewModel.UserGameItems);
			Debug.Assert(view.GroupDescriptions != null, "view.GroupDescriptions != null");
			if (setHeaderStringFormatNull) UserGamesGroupStyle.HeaderStringFormat = null;
			view.GroupDescriptions.Clear();
			if (groupDescription != null) view.GroupDescriptions.Add(groupDescription);
			view.SortDescriptions.Clear();
			foreach (var sortDescription in sortDescriptions)
			{
				view.SortDescriptions.Add(sortDescription);
			}
		}

		private void ToggleLastGroups(string groupName, int count, bool expanded)
		{
			Dispatcher.Invoke(() =>
			{
				CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ViewModel.UserGameItems);
				Debug.Assert(view.GroupDescriptions != null, "view.GroupDescriptions != null");
				if (!view.GroupDescriptions.Any(gd => gd is PropertyGroupDescription pgd && pgd.PropertyName == groupName)) return;
				var expanders = StaticMethods.GetVisualChildren<Expander>(GameFiles);
				for (int index = 0; index < expanders.Count; index++)
				{
					expanders[index].IsExpanded = index >= expanders.Count - count == expanded;
				}
			}, DispatcherPriority.ContextIdle);
		}

		private void ClickDeleteButton(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			Debug.Assert(button != null, nameof(button) + " != null");
			var item = button.DataContext as DisplayEntry;
			Debug.Assert(item != null, nameof(item) + " != null");
			if (item.DeletePrimed)
			{
				ViewModel.DeleteEntry(item);
			}
			else
			{
				item.PrimeDeletion(button);
			}
		}

		private bool _finalizing;
		private bool _finalized;
		private NotifyIcon _trayIcon;

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

		public void SelectTab(Type type)
		{
			var tab = MainTabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Content.GetType() == type);
			MainTabControl.SelectedItem = tab ?? throw new ArgumentNullException(nameof(tab), $"Did not find tab of type {type}");
		}
	}
}