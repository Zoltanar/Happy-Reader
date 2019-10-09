using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
		private readonly MainWindowViewModel _viewModel;

		public MainWindow()
		{
			InitializeComponent();
			_viewModel = new MainWindowViewModel(this);
			DataContext = _viewModel;
			CreateNotifyIcon();
			_viewModel.NotificationEvent += ShowNotification;
			Log.NotificationEvent += ShowLogNotification;
		}

		private void CreateNotifyIcon()
		{
			var contextMenu = new ContextMenuStrip();
			EventHandler open = delegate { Show(); };
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
			_viewModel.ClipboardManager = new ClipboardManager(this);
			await _viewModel.Initialize(watch, GroupByAdded,!Environment.GetCommandLineArgs().Contains("-nh"), !Environment.GetCommandLineArgs().Contains("-ne"));
		}

		private void AddEntry_Click(object sender, RoutedEventArgs e) => CreateAddEntryTab(new Entry());

		private void DropFileOnGamesTab(object sender, DragEventArgs e)
		{
			string file = (e.Data.GetData(DataFormats.FileDrop) as string[])?.First();
			if (string.IsNullOrWhiteSpace(file)) return;
			var ext = Path.GetExtension(file);
			if (!ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
			{
				_viewModel.StatusText = "Dragged file isn't an executable.";
				return;
			}
			var titledImage = _viewModel.AddGameFile(file);
			if (titledImage == null) return;
			((IList<UserGameTile>)GameFiles.ItemsSource).Add(titledImage);
			titledImage.GameDetails(this, null);
		}

		private void GameFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = GameFiles.SelectedItem as UserGameTile;
			var userGame = (UserGame)item?.DataContext;
			if (userGame == null) return;
			_viewModel.HookUserGame(userGame, null);
		}

		private void Debug_Button(object sender, RoutedEventArgs e)
		{
			_viewModel.DebugButton();
		}

		public void ShowLogNotification([NotNull]Log message)
		{
			Console.WriteLine($"Notification - {message.Kind} - {message}");
			NotificationWindow.Launch(message);
		}

		public void ShowNotification(object sender, [NotNull]string message, string title = "Notification")
		{
			Console.WriteLine($"Notification - {title} - {message}");
			NotificationWindow.Launch(title, message);
		}

		public void TabMiddleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Middle) return;
			MainTabControl.Items.Remove((TabItem)((Grid)sender).Parent);
		}

		public void CreateAddEntryTab(Entry initialEntry)
		{
			var tabItem = new TabItem
			{
				Header = "Add Entry",
				Name = "AddEntryControl",
				Content = new AddEntryControl(_viewModel, initialEntry)
			};
			AddTabItem(tabItem);
		}

		public void OpenVNPanel(ListedVN vn)
		{
			var tabItem = new TabItem
			{
				Header = StaticHelpers.TruncateString(vn.Title, 30),
				Name = "VNPanel",
				Content = new VNTab(vn)
			};
			AddTabItem(tabItem);
		}

		public void AddTabItem(TabItem tabItem)
		{
			var header = new Grid();
			header.Children.Add(new TextBlock { Text = (string)tabItem.Header });
			header.MouseDown += TabMiddleClick;
			tabItem.Header = header;
			MainTabControl.Items.Add(tabItem);
			MainTabControl.SelectedItem = tabItem;
			tabItem.Focus();
		}

		private void SetClipboardSize(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			StaticHelpers.GSettings.MaxClipboardSize = (int)((Slider)e.Source).Value;
		}

		private void GroupByProducer(object sender, RoutedEventArgs e)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(_viewModel.UserGameItems);
			PropertyGroupDescription groupDescription = new PropertyGroupDescription("UserGame.VN.Producer");
			UserGamesGroupStyle.HeaderStringFormat = null;
			var sortDescription = new SortDescription(groupDescription.PropertyName, ListSortDirection.Descending);
			view.GroupDescriptions.Clear();
			view.GroupDescriptions.Add(groupDescription);
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(sortDescription);
		}

		private void GroupByMonth(object sender, RoutedEventArgs e)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(_viewModel.UserGameItems);
			PropertyGroupDescription groupDescription = new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.MonthGroupingString)}");
			view.GroupDescriptions.Clear();
			view.GroupDescriptions.Add(groupDescription);
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.MonthGrouping)}", ListSortDirection.Descending));
		}

		private void GroupByName(object sender, RoutedEventArgs e)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(_viewModel.UserGameItems);
			PropertyGroupDescription groupDescription = new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayNameGroup)}");
			view.GroupDescriptions.Clear();
			view.GroupDescriptions.Add(groupDescription);
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayName)}", ListSortDirection.Ascending));
		}

		private void GroupByLastPlayed(object sender, RoutedEventArgs e)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(_viewModel.UserGameItems);
			PropertyGroupDescription groupDescription = new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.LastPlayedDate)}",new LastPlayedConvertor());
			view.GroupDescriptions.Clear();
			view.GroupDescriptions.Add(groupDescription);
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.LastPlayedDate)}", ListSortDirection.Descending));
		}

		private void GroupByTimePlayed(object sender, RoutedEventArgs e)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(_viewModel.UserGameItems);
			PropertyGroupDescription groupDescription = new PropertyGroupDescription($"{nameof(UserGame)}.{nameof(UserGame.TimeOpen)}", new TimeOpenConvertor());
			view.GroupDescriptions.Clear();
			view.GroupDescriptions.Add(groupDescription);
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.TimeOpen)}", ListSortDirection.Descending));
		}
		
		private void GroupByAdded(object sender, RoutedEventArgs e)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(_viewModel.UserGameItems);
			view.GroupDescriptions.Clear();
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.Id)}", ListSortDirection.Descending));
		}

		private class LastPlayedConvertor : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if (!(value is DateTime dt)) return value;
				if (dt == DateTime.MinValue) return "Never";
				var timeSince = DateTime.Now - dt;
				if (timeSince.TotalDays < 3) return "Last 3 days";
				if (timeSince.TotalDays < 7) return "Last week";
				return timeSince.TotalDays < 30 ? "Last month" : "Earlier";
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new NotImplementedException();
		}

		private class TimeOpenConvertor : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if (!(value is TimeSpan time)) return value;
				if (time == TimeSpan.MinValue) return "Never";
				if (time.TotalHours < 0.5) return "<30 Minutes";
				if (time.TotalHours < 1) return "<1 Hour";
				if (time.TotalHours < 3) return "<3 Hours";
				if (time.TotalHours < 8) return "<8 Hours";
				if (time.TotalHours < 20) return "<20 Hours";
				if (time.TotalHours < 50) return "<50 Hours";
				return time.TotalHours < 100 ? "<100 Hours" : ">100 Hours";
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new NotImplementedException();
		}

		private void ClickDeleteButton(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			Debug.Assert(button != null, nameof(button) + " != null");
			var item = button.DataContext as DisplayEntry;
			Debug.Assert(item != null, nameof(item) + " != null");
			if (item.DeletePrimed)
			{
				_viewModel.DeleteEntry(item);
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
			_viewModel.ExitProcedures(this, null);
			_finalized = true;
			Close();
		}

	}
}