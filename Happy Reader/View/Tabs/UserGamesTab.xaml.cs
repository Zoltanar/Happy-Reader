using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Converters;
using Happy_Reader.View.Tiles;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class UserGamesTab : UserControl
	{
		private readonly DispatcherTimer _scrollLabelTimer;

		private UserGamesViewModel ViewModel => (UserGamesViewModel)DataContext; 
		public MainWindowViewModel MainViewModel
		{
			get
			{
				Debug.Assert(Application.Current.MainWindow != null, "Application.Current.MainWindow != null");
				return ((MainWindowViewModel)Application.Current.MainWindow.DataContext);
			}
		}

		public UserGamesTab()
		{
			InitializeComponent();
			_scrollLabelTimer = new DispatcherTimer(new TimeSpan(0, 0, 2), DispatcherPriority.ContextIdle, HideScrollLabel, Dispatcher);
		}

		private void DropFileOnGamesTab(object sender, DragEventArgs e)
		{
			string file = (e.Data.GetData(DataFormats.FileDrop) as string[])?.First();
			if (string.IsNullOrWhiteSpace(file))
			{
				MainViewModel.StatusText = "Dragged item was not a file.";
				return;
			}
			var ext = Path.GetExtension(file);
			if (!ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
			{
				MainViewModel.StatusText = "Dragged file isn't an executable.";
				return;
			}
			var titledImage = ViewModel.AddGameFile(file);
			if (titledImage == null) return;
			((IList<UserGameTile>)GameFiles.ItemsSource).Add(titledImage);
			titledImage.ViewDetails(this, null);
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

		public void GroupByAdded(object sender, RoutedEventArgs e)
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

		private void GroupByVnScore(object sender, RoutedEventArgs e)
		{
			var groupName = $"{nameof(UserGame)}.{nameof(UserGame.VN)}.{nameof(ListedVN.UserVN)}.{nameof(UserVN.Vote)}";
			var groupDescription = new PropertyGroupDescription(groupName, new UserVnToScoreConverter());
			GroupUserGameItems(
				groupDescription,
				new SortDescription(groupName, ListSortDirection.Descending),
				new SortDescription($"{nameof(UserGame)}.{nameof(UserGame.DisplayName)}", ListSortDirection.Ascending));
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
				if (groupName != null && !view.GroupDescriptions.Any(gd => gd is PropertyGroupDescription pgd && pgd.PropertyName == groupName)) return;
				var expanders = StaticMethods.GetVisualChildren<Expander>(GameFiles);
				if (expanders.Count == 0) return;
				if (groupName == null) expanded = !expanders[0].IsExpanded;
				for (int index = 0; index < expanders.Count; index++)
				{
					//if groupName is null, we just toggle all groups to the inverse state of the first group.
					expanders[index].IsExpanded = groupName == null ? expanded : (fromStart ? index >= count != expanded : index >= expanders.Count - count == expanded);
				}
			}, DispatcherPriority.ContextIdle);
		}

		private void ShowLabelOnScrollbar(object sender, ScrollChangedEventArgs e)
		{
			if (ViewModel == null) return;
			if (!(sender is ListBox list))
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
					groupName = groupNameObject == null || groupNameObject == DependencyProperty.UnsetValue ? "Other" : groupNameObject.ToString();
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

		private void ToggleExpandGroups(object sender, RoutedEventArgs e)
		{
			ToggleUserGameGroups(null, 0, true, true);
		}

		private async void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
		{
			var tb = (ToggleButton) sender;
			var state = tb.IsChecked ?? false;
			await ViewModel.LoadUserGames(state);
		}
	}
}
