using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Tiles;
using JetBrains.Annotations;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.ViewModel
{
	public class VNTabViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private int _listedVnPage;
		private bool _finalPage;
#if DEBUG
		private const int PageSize = 100;
#else
        private const int PageSize = 1000;
#endif
		private string _vndbConnectionReply;
		private Brush _vndbConnectionColor;
		private readonly MainWindowViewModel _mainViewModel;
		private Func<VisualNovelDatabase, IEnumerable<ListedVN>> _dbFunction = x => x.LocalVisualNovels;
		private CustomVnFilter _selectedFilter;

		public CoreSettings CSettings => StaticHelpers.CSettings;
		public ObservableCollection<CustomVnFilter> VnFilters { get; }
		public CustomVnFilter PermanentFilter { get; }
		public CustomVnFilter SelectedFilter
		{
			get => _selectedFilter;
			set
			{
				if (_selectedFilter == value) return;
				_selectedFilter = value;
				Task.Run(() => ChangeFilter(_selectedFilter));

			}
		}
		public PausableUpdateList<VNTile> ListedVNs { get; set; } = new PausableUpdateList<VNTile>();
		public int[] AllVNResults { get; private set; }
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

		public VNTabViewModel(MainWindowViewModel mainWindowViewModel, ObservableCollection<CustomVnFilter> vnFilters, CustomVnFilter permanentFilter)
		{
			_mainViewModel = mainWindowViewModel;
			VnFilters = vnFilters;
			PermanentFilter = permanentFilter;
		}

		public async Task Initialize()
		{
			_mainViewModel.StatusText = "Loading VN Database...";
			await Task.Run(() => LocalDatabase = new VisualNovelDatabase(true));
			await _mainViewModel.SetUser(47063, false);
			_mainViewModel.StatusText = "Opening VNDB Connection...";
			await Task.Run(() =>
			{
				Conn = new VndbConnection(VndbConnectionText, _mainViewModel.VndbAdvancedAction, RefreshListedVnsTask());
				Conn.Login(ClientName, ClientVersion);
			});
			_mainViewModel.StatusText = "Loading VN List...";
			await RefreshListedVns();
		}

		public void VndbConnectionText(string text, VndbConnection.MessageSeverity severity)
		{
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

		public async Task RefreshListedVns(bool showAll = false)
		{
			await Task.Run(RefreshListedVnsTask(showAll));
		}

		private Action RefreshListedVnsTask(bool showAll = false)
		{
			return () =>
			{
				var watch = Stopwatch.StartNew();
				_finalPage = false;
				if (showAll) _dbFunction = x => x.LocalVisualNovels;
				_listedVnPage = 1;
				var preFilteredResults = _dbFunction.Invoke(LocalDatabase).OrderByDescending(x => x.ReleaseDate).Select(x => x.VNID).ToArray();
				var filterResults = LocalDatabase.LocalVisualNovels.Where(PermanentFilter.GetFunction()).Select(x => x.VNID).ToArray();
				AllVNResults = preFilteredResults.Where(x => filterResults.Contains(x)).ToArray();
				var firstPage = AllVNResults.Take(PageSize).ToArray();
				var results = LocalDatabase.LocalVisualNovels.Where(x => firstPage.Contains(x.VNID))
					.OrderByDescending(x => x.ReleaseDate).ToList();
				Application.Current.Dispatcher.Invoke(() =>
				{
					ListedVNs.SetRange(results.Select(VNTile.FromListedVN));
					OnPropertyChanged(nameof(CSettings));
					OnPropertyChanged(nameof(ListedVNs));
				});
				Logger.ToDebug($"RefreshListedVns took {watch.Elapsed.ToSeconds()}.");
			};
		}

		public void AddListedVNPage()
		{
			if (_finalPage) return;
			var newPage = AllVNResults.Skip(_listedVnPage * PageSize).Take(PageSize).ToList();
			_listedVnPage++;
			if (newPage.Count < PageSize)
			{
				_finalPage = true;
				if (newPage.Count == 0) return;
			}

			var newTiles = LocalDatabase.LocalVisualNovels.Where(x => newPage.Contains(x.VNID)).Select(VNTile.FromListedVN);
			ListedVNs.AddRange(newTiles);
			OnPropertyChanged(nameof(ListedVNs));
		}

		public async Task UpdateURT()
		{
			await Conn.UpdateURT();
			await Task.Run(() =>
			{
				foreach (var vn in LocalDatabase.LocalVisualNovels)
				{
					vn.UserVN = LocalDatabase.LocalUserVisualNovels.SingleOrDefault(x =>
						x.UserId == CSettings.UserID && x.VNID == vn.VNID);
				}
			});
			var changes = await LocalDatabase.SaveChangesAsync();
			if (changes > 0) await RefreshListedVns();
		}

		public async Task SearchForVN(string text)
		{
			_dbFunction = db => db.LocalVisualNovels.Where(VisualNovelDatabase.ListVNByNameOrAliasFunc(text));
			await RefreshListedVns();
		}

		public async Task UpdateForYear(int year)
		{
			var watch = Stopwatch.StartNew();
			await Conn.UpdateForYear(year);
			Debug.WriteLine($@"{nameof(UpdateForYear)}: {watch.ElapsedMilliseconds}ms");
		}

		public async Task UpdateCharactersForYear(int year)
		{
			var watch = Stopwatch.StartNew();
			await Conn.UpdateCharactersForYear(year);
			Debug.WriteLine($@"{nameof(UpdateCharactersForYear)}: {watch.ElapsedMilliseconds}ms");
		}

		public async Task FetchForYear(int fromYear = 0, int toYear = VndbConnection.VndbAPIMaxYear)
		{
			var watch = Stopwatch.StartNew();
			await Conn.FetchForYear(fromYear, toYear);
			Debug.WriteLine($@"{nameof(FetchForYear)}: {watch.ElapsedMilliseconds}ms");
		}

		public async Task ShowURT()
		{
			_dbFunction = db => db.LocalVisualNovels.Where(vn => vn.UserVNId != null);
			await RefreshListedVns();
		}

		public async Task ShowTagged(DumpFiles.WrittenTag tag)
		{
			var watch = Stopwatch.StartNew();
			_dbFunction = db => db.Tags.Where(t => tag.AllIDs.Contains(t.TagId)).Select(x => x.ListedVN).Distinct();
			await RefreshListedVns();
			Debug.WriteLine($@"{nameof(ShowTagged)}: {watch.ElapsedMilliseconds}ms");
		}

		public async Task ChangeFilter(CustomVnFilter item)
		{
			_dbFunction = db => db.LocalVisualNovels.Where(item.GetFunction());
			await RefreshListedVns();
		}

		public void ToggleFiltersJapanese()
		{
			foreach (var filter in VnFilters)
			{
				var vnFilter = filter.AndFilters.FirstOrDefault(x => x.Type == VnFilterType.OriginalLanguage);
				if (vnFilter == null) filter.AndFilters.Add(new VnFilter(VnFilterType.OriginalLanguage, "ja"));
				else filter.AndFilters.Remove(vnFilter);
			}
		}
	}
}