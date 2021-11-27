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
	
	public partial class MergeGameControl : UserControl
	{
		public UserGame UserGame { get; }
		public MergeGame[] MergeGames { get; private set; }
		public MergeGame[] MergeResults { get; private set; }
		public string GameName => UserGame.DisplayName;
		public Action<bool,MergeGame[]> Callback { get; set; }

		public MergeGameControl(UserGame userGame, Action<bool, MergeGame[]> callback)
		{
			UserGame = userGame;
			Callback = callback;
			InitializeComponent();
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			MergeResults = MergeGames.Where(g => g.Selected).ToArray();
			Callback(true, MergeResults);
		}


		private void Cancel(object sender, RoutedEventArgs e) => Callback(false, null);

		private async void DataGridLoaded(object sender, RoutedEventArgs e)
		{
			MergeGame[] mergeGames = null;
			Debug.Assert(Dispatcher != null, nameof(Dispatcher) + " != null");
			await Dispatcher.InvokeAsync(() =>
			{
				mergeGames = StaticMethods.Data.UserGames.Where(g => g.Id != UserGame.Id).Select(g => new MergeGame(g)).OrderBy(g => g.Name).ToArray();
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
			var hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
			var dataGridRow = hitTestResult.VisualHit.FindParent<DataGridRow>();
			if (dataGridRow == null) return;
			var mergeGame = (MergeGame)dataGridRow.Item;
			mergeGame.Selected = !mergeGame.Selected;
		}
	}
}
