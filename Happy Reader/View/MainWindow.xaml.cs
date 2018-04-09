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
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Tabs;
using Happy_Reader.View.Tiles;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolTipIcon = System.Windows.Forms.ToolTipIcon;
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
		private readonly NotifyIcon _trayIcon;

		public MainWindow()
		{
			InitializeComponent();
			_viewModel = new MainWindowViewModel(this);
			DataContext = _viewModel;
			_trayIcon = BuildNotifyIcon();
			_viewModel.NotificationEvent += ShowNotification;
			Log.NotificationEvent += ShowNotification;
		}

		private NotifyIcon BuildNotifyIcon()
		{
			var contextMenu = new ContextMenuStrip();
			EventHandler open = delegate { Show(); };
			contextMenu.Items.Add(new ToolStripMenuItem("Open", null, open));
			contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, Exit));
			// ReSharper disable once PossibleNullReferenceException
			Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/logo-hr.ico")).Stream;
			var trayIcon = new NotifyIcon
			{
				Icon = new System.Drawing.Icon(iconStream),
				ContextMenuStrip = contextMenu,
				Visible = true
			};
			trayIcon.DoubleClick += open;
			return trayIcon;
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Stopwatch watch = Stopwatch.StartNew();
			GroupByMonth(null, null);
			_viewModel.ClipboardManager = new ClipboardManager(this);
			await _viewModel.Initialize(watch);
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

		public void ShowNotification(object sender, [NotNull]string message, string title = "Notification")
		{
			Console.WriteLine($"Log - {title} - {message}");
			_trayIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
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

		private void ChangeIthPath(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			string directory = Path.GetDirectoryName(StaticHelpers.GSettings.IthPath);
			while (!Directory.Exists(directory))
			{
				directory = directory == null ? Environment.CurrentDirectory : Directory.GetParent(directory).FullName;
			}
			dialog.InitialDirectory = directory;
			var result = dialog.ShowDialog();
			if (result ?? false)
			{
				IthPathBox.Text = dialog.FileName;
				StaticHelpers.GSettings.IthPath = dialog.FileName;
			}
		}

		private void SetClipboardSize(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			StaticHelpers.GSettings.MaxClipboardSize = (int)((Slider)e.Source).Value;
		}

		private void GroupByProducer(object sender, RoutedEventArgs e)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(_viewModel.UserGameItems);
			PropertyGroupDescription groupDescription = new PropertyGroupDescription("UserGame.VN.Producer"/*,new IdFromProducerConverter()*/);
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
			UserGamesGroupStyle.HeaderStringFormat = "{0:MMMM} {0:yyyy}";
			PropertyGroupDescription groupDescription = new PropertyGroupDescription("UserGame.VN.ReleaseDate", new MonthFromDateTimeConverter());
			view.GroupDescriptions.Clear();
			view.GroupDescriptions.Add(groupDescription);
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription(groupDescription.PropertyName, ListSortDirection.Descending));
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
			_viewModel.ExitProcedures(this, null);
			_finalized = true;
			Close();
		}
	}
}