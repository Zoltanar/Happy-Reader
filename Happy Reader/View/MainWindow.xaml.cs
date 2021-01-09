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
using System.Windows.Threading;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Converters;
using Happy_Reader.View.Tabs;
using Happy_Reader.View.Tiles;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;

namespace Happy_Reader.View
{
	public partial class MainWindow
	{
		public MainWindowViewModel ViewModel { get; }

		public MainWindow()
		{
			InitializeComponent();
			ViewModel = new MainWindowViewModel();
			DataContext = ViewModel;
			CreateNotifyIcon();
			ViewModel.NotificationEvent += ShowNotification;
			Log.NotificationEvent += ShowLogNotification;
			_scrollLabelTimer = new DispatcherTimer(new TimeSpan(0, 0, 2), DispatcherPriority.ContextIdle, HideScrollLabel, Dispatcher);
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
			var logVerbose = commandLineArgs.Contains("-lv");
			await ViewModel.Initialize(watch, GroupByAdded, !noHook, !noEntries, noTranslation, logVerbose);
		}

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
			var vnEntries = ViewModel.UserGameItems
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

		private void AddTabItem(TabItem tabItem, BindingBase headerBinding)
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
					VNTab _ => Brushes.HotPink,
					UserGameTab _ => Brushes.DarkKhaki,
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

		private void GroupByProducer(object sender, RoutedEventArgs e)
		{
			var groupName = $"{nameof(UserGame)}.{nameof(UserGame.VN)}.{nameof(ListedVN.Producer)}.{nameof(ListedProducer.Name)}";
			GroupUserGameItems(
				new PropertyGroupDescription(groupName),
				new SortDescription(groupName, ListSortDirection.Ascending),
				new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.VN)}.{nameof(ListedVN.Title)}", ListSortDirection.Ascending));
			ToggleUserGameGroups(groupName, 1, false, true);
		}

		private void GroupByReleaseMonth(object sender, RoutedEventArgs e)
		{
			var groupName = $@"{nameof(UserGame)}.{nameof(UserGame.VN)}.{nameof(ListedVN.ReleaseDate)}";
			GroupUserGameItems(
				new PropertyGroupDescription(groupName, new DateToMonthStringConverter()),
				new SortDescription(groupName, ListSortDirection.Descending));
		}

		private void GroupByName(object sender, RoutedEventArgs e)
		{
			GroupUserGameItems(
				new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayNameGroup)}"),
				new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayName)}", ListSortDirection.Ascending));
		}

		private void GroupByLastPlayed(object sender, RoutedEventArgs e)
		{
			var groupName = $@"{nameof(UserGame)}.{nameof(UserGame.LastPlayedDate)}";
			GroupUserGameItems(
				new PropertyGroupDescription(groupName, new LastPlayedConverter()),
				new SortDescription(groupName, ListSortDirection.Descending));
			ToggleUserGameGroups(groupName, 2, false, false);
		}

		private void GroupByTimePlayed(object sender, RoutedEventArgs e)
		{
			var groupName = $@"{nameof(UserGame)}.{nameof(UserGame.TimeOpen)}";
			GroupUserGameItems(
				new PropertyGroupDescription(groupName, new TimeOpenConverter()),
				new SortDescription(groupName, ListSortDirection.Descending));
			ToggleUserGameGroups(groupName, 2, true, false);
		}

		private void GroupByAdded(object sender, RoutedEventArgs e)
		{
			GroupUserGameItems(
				null,
				new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.Id)}", ListSortDirection.Descending));
		}

		private void GroupByTag(object sender, RoutedEventArgs e)
		{
			var groupDescription = new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.Tag)}", new TagConverter());
			GroupUserGameItems(
				groupDescription,
				new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.TagSort)}", ListSortDirection.Descending),
			new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayName)}", ListSortDirection.Ascending));
		}

		private void GroupByVnLabel(object sender, RoutedEventArgs e)
		{
			var groupName = $"{nameof(UserGame)}.{nameof(UserGame.VN)}.{nameof(ListedVN.UserVN)}.{nameof(UserVN.PriorityLabel)}";
			var groupDescription = new PropertyGroupDescription(groupName, new UserVnToLabelConverter());
			GroupUserGameItems(
				groupDescription,
				new SortDescription(groupName, ListSortDirection.Ascending),
				new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayName)}", ListSortDirection.Ascending));
			ToggleUserGameGroups(groupName, 2, true, true);
		}
		private void GroupUserGameItems(GroupDescription groupDescription, params SortDescription[] sortDescriptions)
		{
			if (ViewModel == null) return;
			var view = CollectionViewSource.GetDefaultView(ViewModel.UserGameItems);
			Debug.Assert(view.GroupDescriptions != null, "view.GroupDescriptions != null");
			view.GroupDescriptions.Clear();
			if (groupDescription != null) view.GroupDescriptions.Add(groupDescription);
			view.SortDescriptions.Clear();
			foreach (var sortDescription in sortDescriptions)
			{
				view.SortDescriptions.Add(sortDescription);
			}
		}

		private void ToggleUserGameGroups(string groupName, int count, bool expanded, bool fromStart)
		{
			Dispatcher.Invoke(() =>
			{
				var view = CollectionViewSource.GetDefaultView(ViewModel.UserGameItems);
				Debug.Assert(view.GroupDescriptions != null, "view.GroupDescriptions != null");
				if (!view.GroupDescriptions.Any(gd => gd is PropertyGroupDescription pgd && pgd.PropertyName == groupName)) return;
				var expanders = StaticMethods.GetVisualChildren<Expander>(GameFiles);
				for (int index = 0; index < expanders.Count; index++)
				{
					expanders[index].IsExpanded = (fromStart ? index >= count != expanded : index >= expanders.Count - count == expanded);
				}
			}, DispatcherPriority.ContextIdle);
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

		private void ShowLabelOnScrollbar(object sender, ScrollChangedEventArgs e)
		{
			var list = sender as ListBox;
			if (list == null)
			{
				ScrollLabel.Text = string.Empty;
				ScrollBorder.Visibility = Visibility.Hidden;
				return;
			}
			var hitTest = VisualTreeHelper.HitTest(list, new Point(5, 5/*e.VerticalOffset*/));
			if (hitTest == null)
			{
				ScrollLabel.Text = string.Empty;
				ScrollBorder.Visibility = Visibility.Hidden;
				return;
			}
			var lbi = GetVisualItemOfType<ListBoxItem>(hitTest.VisualHit, list);
			var tile = lbi?.DataContext as UserGameTile;
			var game = tile?.UserGame;
			var view = CollectionViewSource.GetDefaultView(ViewModel.UserGameItems);
			Debug.Assert(view.GroupDescriptions != null, "view.GroupDescriptions != null");
			var groupDescription = view.GroupDescriptions.FirstOrDefault();
			if (game == null && groupDescription == null)
			{
				ScrollBorder.Visibility = Visibility.Hidden;
				ScrollLabel.Text = string.Empty;
				return;
			}
			string textForLabel;
			if (groupDescription == null) textForLabel = $"Game: {game.DisplayName}";
			else
			{
				string groupName;
				if (game != null)
				{
					var groupNameObject = groupDescription.GroupNameFromItem(tile, 0, System.Globalization.CultureInfo.CurrentCulture);
					groupName = groupNameObject == DependencyProperty.UnsetValue ? "Other" : groupNameObject.ToString();
				}
				else
				{
					var gi = GetVisualItemOfType<GroupItem>(hitTest.VisualHit, list);
					if (gi == null)
					{
						ScrollBorder.Visibility = Visibility.Hidden;
						ScrollLabel.Text = string.Empty;
						return;
					}
					groupName = gi.ToString()?.Replace($"{gi.GetType()}: ", "").Trim();
				}

				textForLabel = $"Group: {groupName}";
			}
			ScrollBorder.Visibility = Visibility.Visible;
			ScrollLabel.Text = textForLabel;
			if (_scrollLabelTimer.IsEnabled) _scrollLabelTimer.Stop();
			_scrollLabelTimer.Start();
		}

		private readonly DispatcherTimer _scrollLabelTimer;

		private void HideScrollLabel(object sender, EventArgs eventArgs)
		{
			ScrollBorder.Visibility = Visibility.Hidden;
			_scrollLabelTimer.Stop();
		}

		private T GetVisualItemOfType<T>(object originalSource, object parent) where T : DependencyObject
		{
			if (!(originalSource is DependencyObject depObj)) return null;
			// go up the visual hierarchy until we find the list view item the click came from  
			// the click might have been on the grid or column headers so we need to cater for this  
			DependencyObject current = depObj;
			while (current != null && current != parent)
			{
				if (current is T item) return item;
				current = VisualTreeHelper.GetParent(current);
			}
			return null;
		}
	}
}