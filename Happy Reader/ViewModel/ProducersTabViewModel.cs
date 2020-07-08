using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.ViewModel
{
	public class ProducersTabViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private int _listedProducersPage;
		private bool _finalPage;
#if DEBUG
		private const int PageSize = 100;
#else
        private const int PageSize = 1000;
#endif
		private string _vndbConnectionReply;
		private Brush _vndbConnectionColor;
		private readonly MainWindowViewModel _mainViewModel;
		private Func<VisualNovelDatabase, IEnumerable<ListedProducer>> _dbFunction = x => x.CurrentUser?.FavoriteProducers ?? x.Producers.AsEnumerable();
		private Func<ListedProducer, string> _order => producer => producer.Name;

		public PausableUpdateList<ListedProducer> ListedProducers { get; set; } = new PausableUpdateList<ListedProducer>();
		public int[] AllProducerResults { get; private set; }
		public string VndbConnectionReply
		{
			get => _vndbConnectionReply;
			set { _vndbConnectionReply = value; OnPropertyChanged(); }
		}
		public Brush VndbConnectionColor
		{
			get => _vndbConnectionColor;
			set { _vndbConnectionColor = value; OnPropertyChanged(); }
		}

		public ProducersTabViewModel(MainWindowViewModel mainWindowViewModel)
		{
			_mainViewModel = mainWindowViewModel;
		}

		public async Task Initialize()
		{
			_mainViewModel.StatusText = "Loading Producers List...";
			await RefreshListedProducers();
		}

		public void VndbConnectionText(string text, VndbConnection.MessageSeverity severity)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				VndbConnectionReply = text;
				switch (severity)
				{
					case VndbConnection.MessageSeverity.Normal:
						VndbConnectionColor = new SolidColorBrush(Colors.Black);
						return;
					case VndbConnection.MessageSeverity.Warning:
						VndbConnectionColor = new SolidColorBrush(Colors.Yellow);
						return;
					case VndbConnection.MessageSeverity.Error:
						VndbConnectionColor = new SolidColorBrush(Colors.Red);
						return;
				}
			});
		}

		public async Task RefreshListedProducers(bool showAll = false)
		{
			await Task.Run(RefreshListedProducersTask(showAll));
		}

		private Action RefreshListedProducersTask(bool showAll)
		{
			return () =>
			{
				var watch = Stopwatch.StartNew();
				_finalPage = false;
				_listedProducersPage = 1;
				if (showAll) _dbFunction = x => x.CurrentUser?.FavoriteProducers ?? x.Producers.AsEnumerable();
				AllProducerResults = _dbFunction.Invoke(LocalDatabase).OrderBy(_order).Select(x => x.ID).ToArray();
				var firstPage = AllProducerResults.Take(PageSize).ToArray();
				List<ListedProducer> results = LocalDatabase.Producers.WithKeyIn(firstPage).OrderBy(_order).ToList();
				if (LocalDatabase.CurrentUser != null)
				{
					//var m = LocalDatabase.UserListedProducers.Where(x => x.User_Id == LocalDatabase.CurrentUser.Id).ToArray();
					var now = DateTime.UtcNow;
					foreach (var listedProducer in results) listedProducer.SetFavoriteProducerData(LocalDatabase, now);
				}
				if (AllProducerResults.Length <= PageSize) _finalPage = true;
				Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
				Application.Current.Dispatcher.Invoke(() =>
				{
					ListedProducers.SetRange(results);
					OnPropertyChanged(nameof(CSettings));
					OnPropertyChanged(nameof(ListedProducers));
				});
				Logger.ToDebug($"RefreshListedVns took {watch.Elapsed.ToSeconds()}.");
			};
		}

		public void AddListedProducersPage()
		{
			if (_finalPage) return;
			var newPage = AllProducerResults.Skip(_listedProducersPage * PageSize).Take(PageSize).ToList();
			_listedProducersPage++;
			if (newPage.Count < PageSize)
			{
				_finalPage = true;
				if (newPage.Count == 0) return;
			}

			var newProducers = LocalDatabase.Producers.WithKeyIn(newPage).OrderBy(_order).ToArray();

			if (LocalDatabase.CurrentUser != null)
			{
				var now = DateTime.UtcNow;
				//var m = LocalDatabase.UserListedProducers.Where(x => x.User_Id == LocalDatabase.CurrentUser.Id).ToArray();
				foreach (var listedProducer in newProducers) listedProducer.SetFavoriteProducerData(LocalDatabase, now);
			}
			ListedProducers.AddRange(newProducers);
			OnPropertyChanged(nameof(ListedProducers));
		}

		public async Task SearchForProducer(string text)
		{
			_dbFunction = db => db.Producers.Where(x => x.Name.ToLowerInvariant().Contains(text.ToLowerInvariant()));
			await RefreshListedProducers();
		}
	}
}