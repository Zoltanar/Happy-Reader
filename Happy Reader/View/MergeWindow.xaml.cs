using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Happy_Reader.Database;
using JetBrains.Annotations;

namespace Happy_Reader.View
{
	public class MergeGame : INotifyPropertyChanged 
	{
		private bool _selected;

		public UserGame UserGame { get; }

		public bool Selected
		{
			get => _selected;
			set
			{
				_selected = value;
				OnPropertyChanged();
			}
		}

		public BitmapImage Image { get; }
		public string Producer { get; }
		public string Name { get; }
		public string TimePlayed { get; }
		public bool FileExists { get; }

		public MergeGame(UserGame game)
		{
			UserGame = game;
			Selected = false;
			Image = game.Image;
			Producer = game.VN?.Producer?.Name ?? "";
			Name = game.DisplayName;
			TimePlayed = game.TimeOpen.ToHumanReadable();
			FileExists = File.Exists(game.FilePath);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	/// <summary>
	/// Interaction logic for MergeWindow.xaml
	/// </summary>
	public partial class MergeWindow : Window
	{
		public UserGame UserGame { get; }
		public MergeGame[] MergeGames { get; private set; }
		public MergeGame[] MergeResults { get; private set; }
		public string GameName => UserGame.DisplayName;

		public MergeWindow(UserGame userGame)
		{
			UserGame = userGame;
			InitializeComponent();
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			MergeResults = MergeGames.Where(g => g.Selected).ToArray();
		}


		private void Cancel(object sender, RoutedEventArgs e) => Close();

		private async void DataGridLoaded(object sender, RoutedEventArgs e)
		{
			MergeGame[] mergeGames = null;
			Debug.Assert(Dispatcher != null, nameof(Dispatcher) + " != null");
			await Dispatcher.InvokeAsync(() =>
			{
				mergeGames = StaticMethods.Data.UserGames.Local.Where(g => g.Id != UserGame.Id).ToArray()
					.Select(g => new MergeGame(g)).OrderBy(g => g.Name).ToArray();
			}, DispatcherPriority.Background);
			MergeGames = mergeGames;
			// ReSharper disable once PossibleNullReferenceException
			MergeDataGrid.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget();
			var list = MergeGames.Select(g => g.Name).ToList();
			list.Add(UserGame.DisplayName);
			list.Sort();
			var index = list.IndexOf(UserGame.DisplayName);
			((DataGrid)sender).ScrollIntoView(MergeGames[Math.Max(0, index - 1)]);

		}

		private void DataGridMouseLeftUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var dataGrid = (DataGrid)sender;
			HitTestResult hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
			DataGridRow dataGridRow = GetParentOfType<DataGridRow>(hitTestResult.VisualHit);
			if (dataGridRow == null) return;
			var mergeGame = (MergeGame)dataGridRow.Item;
			mergeGame.Selected = !mergeGame.Selected;
		}

		public static T GetParentOfType<T>(DependencyObject element) where T : DependencyObject
		{
			while (true)
			{
				var type = typeof(T);
				if (element == null) return null;
				DependencyObject parent = VisualTreeHelper.GetParent(element);
				if (parent == null && ((FrameworkElement)element).Parent != null) parent = ((FrameworkElement)element).Parent;
				if (parent == null) return null;
				if (parent.GetType() == type || parent.GetType().IsSubclassOf(type)) return parent as T;
				element = parent;
			}
		}
	}
}
