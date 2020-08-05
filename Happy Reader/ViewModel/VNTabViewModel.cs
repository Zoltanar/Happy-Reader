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
using System.Windows.Threading;
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
		public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private int _listedVnPage;
		private bool _finalPage;
#if DEBUG
		private const int PageSize = 50;
#else
		private const int PageSize = 100;
#endif
		private string _replyText;
		private Brush _replyColor;
		private Brush _vndbConnectionBackground;
		private Brush _vndbConnectionForeground;
		private string _vndbConnectionStatus;
		private readonly MainWindowViewModel _mainViewModel;
		private NamedFunction _dbFunction = new NamedFunction(x => x.VisualNovels, "All");
		private Func<IEnumerable<ListedVN>, IEnumerable<ListedVN>> _ordering = lvn => lvn.OrderByDescending(x => x.ReleaseDate);
		private CustomVnFilter _selectedFilter;
		private bool _isBlacklisted;

		public ObservableCollection<NamedFunction> History { get; } = new ObservableCollection<NamedFunction>();

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
		public string ReplyText
		{
			get => _replyText;
			set { _replyText = value; OnPropertyChanged(); }
		}

		public bool IsBlacklisted
		{
			get => _isBlacklisted;
			set { _isBlacklisted = value; OnPropertyChanged(); }
		}

		public Brush ReplyColor
		{
			get => _replyColor;
			set { _replyColor = value; OnPropertyChanged(); }
		}

		public Brush VndbConnectionForeground
		{
			get => _vndbConnectionForeground;
			set { _vndbConnectionForeground = value; OnPropertyChanged(); }
		}
		public Brush VndbConnectionBackground
		{
			get => _vndbConnectionBackground;
			set { _vndbConnectionBackground = value; OnPropertyChanged(); }
		}

		public string VndbConnectionStatus
		{
			get => _vndbConnectionStatus;
			set { _vndbConnectionStatus = value; OnPropertyChanged(); }
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
			_mainViewModel.SetUser(CSettings.UserID);
			_mainViewModel.StatusText = "Opening VNDB Connection...";
			await Task.Run(() =>
			{
				Conn = new VndbConnection(SetReplyText, _mainViewModel.VndbAdvancedAction, RefreshListedVnsOnAdded, AskForNonSsl, ChangeConnectionStatus);
				var password = LoadPassword();
				Conn.Login(password != null
					? new VndbConnection.LoginCredentials(ClientName, ClientVersion, CSettings.Username, password)
					: new VndbConnection.LoginCredentials(ClientName, ClientVersion), false);
			});
			_mainViewModel.StatusText = "Loading VN List...";
			await RefreshListedVns();
		}

		private void ChangeConnectionStatus(VndbConnection.APIStatus status)
		{
			Dispatcher.CurrentDispatcher.Invoke(() =>
			{
				switch (status)
				{
					case VndbConnection.APIStatus.Ready:
						if (Environment.GetCommandLineArgs().Contains("-debug")) Logger.ToFile($"{Conn.ActiveQuery.ActionName} Ended");
						if (Conn.ActiveQuery != null) Conn.ActiveQuery.Completed = true;
						VndbConnectionStatus = status.ToString();
						VndbConnectionForeground = Brushes.Black;// SolidColorBrush new SolidColorBrush(Colors.Black);
						VndbConnectionBackground = Brushes.LightGreen; //new SolidColorBrush(Colors.LightGreen);
						break;
					case VndbConnection.APIStatus.Busy:
						if (Environment.GetCommandLineArgs().Contains("-debug")) Logger.ToFile($"{Conn.ActiveQuery.ActionName} Started");
						VndbConnectionStatus = $@"{status} ({Conn.ActiveQuery.ActionName})";
						VndbConnectionForeground = new SolidColorBrush(Colors.Red);
						VndbConnectionBackground = new SolidColorBrush(Colors.Khaki);
						break;
					case VndbConnection.APIStatus.Throttled:
						if (Environment.GetCommandLineArgs().Contains("-debug")) Logger.ToFile($"{Conn.ActiveQuery.ActionName} Throttled");
						VndbConnectionStatus = $@"{status} ({Conn.ActiveQuery.ActionName})";
						VndbConnectionForeground = new SolidColorBrush(Colors.DarkRed);
						VndbConnectionBackground = new SolidColorBrush(Colors.Khaki);
						break;
					case VndbConnection.APIStatus.Error:
						VndbConnectionStatus = $@"{status} ({Conn.ActiveQuery.ActionName})";
						VndbConnectionForeground = new SolidColorBrush(Colors.Black);
						VndbConnectionBackground = new SolidColorBrush(Colors.Red);
						Conn.Close();
						break;
					case VndbConnection.APIStatus.Closed:
						VndbConnectionStatus = $@"{status} ({Conn.ActiveQuery.ActionName})";
						VndbConnectionForeground = new SolidColorBrush(Colors.White);
						VndbConnectionBackground = new SolidColorBrush(Colors.Black);
						break;
				}
			});
		}

		public void SetReplyText(string text, VndbConnection.MessageSeverity severity)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				ReplyText = text;
				switch (severity)
				{
					case VndbConnection.MessageSeverity.Normal:
						ReplyColor = new SolidColorBrush(Colors.Black);
						return;
					case VndbConnection.MessageSeverity.Warning:
						ReplyColor = new SolidColorBrush(Colors.DarkKhaki);
						return;
					case VndbConnection.MessageSeverity.Error:
						ReplyColor = new SolidColorBrush(Colors.Red);
						return;
				}
			});
		}

		public async Task RefreshListedVns()
		{
			var watch = Stopwatch.StartNew();
			_finalPage = false;
			_listedVnPage = 1;
			Application.Current.Dispatcher.Invoke(ResolveHistory);
			SetReplyText("Loading Results...", VndbConnection.MessageSeverity.Warning);
			//await db communication
			var results = await Task.Run(() =>
			{
				var vns = _dbFunction.SelectAndInvoke(LocalDatabase);
				OnPropertyChanged(nameof(SelectedFunctionIndex));
				if (!_dbFunction.AlwaysIncludeBlacklisted && !IsBlacklisted) vns = vns.Where(vn => !(vn.UserVN?.Blacklisted ?? false));
				var preFilteredResults = _ordering(vns).ToArray();
				var filterResults = LocalDatabase.VisualNovels.Where(PermanentFilter.GetFunction()).Select(x => x.VNID).ToArray();
				AllVNResults = _ordering(preFilteredResults.Where(x => filterResults.Contains(x.VNID))).Select(x => x.VNID).ToArray();
				var firstPage = AllVNResults.Take(PageSize).ToArray();
				return _ordering(LocalDatabase.VisualNovels.WithKeyIn(firstPage)).ToList();
			});
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				ListedVNs.SetRange(results.Select(VNTile.FromListedVN));
				OnPropertyChanged(nameof(CSettings));
				OnPropertyChanged(nameof(ListedVNs));
				OnPropertyChanged(nameof(AllVNResults));
			});
			SetReplyText("", VndbConnection.MessageSeverity.Normal);
			Logger.ToDebug($"RefreshListedVns took {watch.Elapsed.ToSeconds()}.");

			void ResolveHistory()
			{
				var indexOfNew = History.ToList().FindIndex(i => i == _dbFunction);
				if (SelectedFunctionIndex == -1) History.Add(_dbFunction);
				else if (indexOfNew == -1)
				{
					//clear forward history
					for (int i = History.Count - 1; i > SelectedFunctionIndex; i--)
					{
						History.RemoveAt(i);
					}
					History.Add(_dbFunction);
				}
				OnPropertyChanged(nameof(BackEnabled));
				OnPropertyChanged(nameof(History));
			}
		}

		private void RefreshListedVnsOnAdded(List<int> titlesAdded)
		{
			_dbFunction = new NamedFunction(db => db.VisualNovels.Where(vn => titlesAdded.Contains(vn.VNID)), "Titles Added");
			Dispatcher.CurrentDispatcher.InvokeAsync(RefreshListedVns);
		}

		private bool AskForNonSsl()
		{
			var messageResult = System.Windows.Forms.MessageBox.Show(@"Connection to VNDB failed, do you wish to try without SSL?",
				@"Connection Failed", System.Windows.Forms.MessageBoxButtons.YesNo);
			return messageResult == System.Windows.Forms.DialogResult.Yes;
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
			var watch = Stopwatch.StartNew();
			var newTiles = LocalDatabase.VisualNovels.WithKeyIn(newPage).OrderByDescending(vn => vn.ReleaseDate).Select(VNTile.FromListedVN).ToList();
			Logger.ToDebug($"[{nameof(AddListedVNPage)}] Creating VN tiles: {watch.Elapsed.ToSeconds()}.");
			ListedVNs.AddRange(newTiles);
			OnPropertyChanged(nameof(ListedVNs));
			OnPropertyChanged(nameof(AllVNResults));
		}

		public async Task UpdateURT()
		{
			var changes = await Conn.UpdateURT();
			if (changes > 0) await RefreshListedVns();
		}

		public async Task SearchForVN(string text)
		{
			var vns = LocalDatabase.VisualNovels.Where(VisualNovelDatabase.SearchForVN(text)).ToList();
			if (!vns.Any())
			{
				SetReplyText($"Found no results for '{text}'", VndbConnection.MessageSeverity.Normal);
				return;
			}
			_dbFunction = new NamedFunction(db => db.VisualNovels.Where(VisualNovelDatabase.SearchForVN(text)), "Search For VN", true);
			await RefreshListedVns();
		}

		public async Task ShowTagged(DumpFiles.WrittenTag tag)
		{
			var watch = Stopwatch.StartNew();
			_dbFunction = new NamedFunction(
				db => db.VisualNovels.WithKeyIn(db.Tags.Where(t => tag.AllIDs.Contains(t.TagId)).Select(x => x.ListedVN_VNID).Distinct().ToArray()),
				$"Tag {tag.Name}");
			await RefreshListedVns();
			Debug.WriteLine($@"{nameof(ShowTagged)}: {watch.ElapsedMilliseconds}ms");
		}

		public async Task ChangeFilter(CustomVnFilter item)
		{
			_dbFunction = new NamedFunction(db => db.VisualNovels.Where(item.GetFunction()), item.Name);
			await RefreshListedVns();
		}

		public async Task<bool> ChangeVNStatus(ListedVN vn, HashSet<UserVN.LabelKind> labels)
		{
			return await Conn.ChangeVNStatus(vn, labels);
		}

		public async Task<bool> ChangeVote(ListedVN vn, int? vote)
		{
			return await Conn.ChangeVote(vn, vote);
		}

		public async Task<bool> UpdateVN(ListedVN vn)
		{
			return await Conn.UpdateVN(vn);
		}

		public async Task ShowForProducer(ListedProducer vnProducer)
		{
			_dbFunction = new NamedFunction(db => db.VisualNovels.Where(vn => vn.Producer == vnProducer),
				$"Producer {vnProducer.Name}",
				true);
			await RefreshListedVns();
		}

		public async Task ShowForCharacter(CharacterItem character)
		{
			_dbFunction = new NamedFunction(
				db => db.VisualNovels.WithKeyIn(db.Characters[character.ID].VisualNovels.Select(cvn => cvn.VNId).ToArray()),
				$"Character {character.Name}",
				true);
			await RefreshListedVns();
		}

		public async Task ShowSuggested()
		{
			var scoredKeys = LocalDatabase.VisualNovels.Select(v => v.VNID).ToArray();
			_dbFunction = new NamedFunction(db => db.VisualNovels.WithKeyIn(scoredKeys), "Suggested");
			_ordering = lvn => lvn.OrderByDescending(vn => vn.Suggestion.Score);
			await RefreshListedVns();
		}
		private SuggestionScorer _suggestionScorer;

		public SuggestionScorer SuggestionScorer
		{
			get
			{
				if (_suggestionScorer != null) return _suggestionScorer;
				_suggestionScorer = new SuggestionScorer(
					StaticHelpers.CSettings.GetTagScoreDictionary(),
					StaticHelpers.CSettings.GetTraitScoreDictionary(),
					LocalDatabase);
				return _suggestionScorer;
			}
		}

		public IEnumerable<ListedProducer> ProducerList => LocalDatabase?.Producers?.AsEnumerable();

		public bool BackEnabled => History.ToList().FindIndex(i => i.Selected) > 0;

		public int SelectedFunctionIndex => History.ToList().FindIndex(i => i.Selected);

		public async Task SortByRecommended()
		{
			_ordering = lvn => lvn.OrderByDescending(vn => vn.Suggestion?.Score ?? 0d)
				.ThenBy(vn => vn.UserVN == null ? 4 :
					vn.UserVN.Labels.Contains(UserVN.LabelKind.WishlistHigh) ? 1 :
					vn.UserVN.Labels.Contains(UserVN.LabelKind.WishlistMedium) ? 2 :
					vn.UserVN.Labels.Contains(UserVN.LabelKind.WishlistLow) ? 3 : 4)
				.ThenByDescending(x => x.ReleaseDate);
			await RefreshListedVns();
		}

		public async Task SortByMyScore()
		{
			_ordering = lvn => lvn.OrderByDescending(vn => vn.UserVN?.Vote);
			await RefreshListedVns();
		}

		public async Task SortByRating()
		{
			_ordering = lvn => lvn.OrderByDescending(vn => vn.Rating);
			await RefreshListedVns();
		}

		public async Task SortByReleaseDate()
		{
			_ordering = lvn => lvn.OrderByDescending(vn => vn.ReleaseDate);
			await RefreshListedVns();
		}

		public async Task SortByTitle()
		{
			_ordering = lvn => lvn.OrderBy(vn => vn.Title);
			await RefreshListedVns();
		}

		public async Task ShowAll()
		{
			_dbFunction = new NamedFunction(db => db.VisualNovels, "All");
			await RefreshListedVns();
		}

		public async Task BrowseHistory(NamedFunction item)
		{
			if (_dbFunction == item) return;
			_dbFunction = item;
			await RefreshListedVns();
		}
	}
}