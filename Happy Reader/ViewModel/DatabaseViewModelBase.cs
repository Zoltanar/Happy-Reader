using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
	public abstract class DatabaseViewModelBase<T> : INotifyPropertyChanged where T : IDataItem<int>, new()
	{
		private const int PageSize = 50;
		private int _currentPage;
		private bool _finalPage;
		private string _replyText;
		private Brush _replyColor;
		private Brush _vndbConnectionBackground;
		private Brush _vndbConnectionForeground;
		private string _vndbConnectionStatus;
		private readonly MainWindowViewModel _mainViewModel;
		protected abstract Func<VisualNovelDatabase, DACollection<int,T>> GetAll { get; }
		protected abstract Func<T, bool> IsBlacklistedFunction { get; }
		protected abstract Func<string, Func<T, bool>> SearchByText { get; }
		protected abstract Func<T, ListedProducer> GetProducer { get; }
		protected abstract Func<T, UserControl> GetTile { get; }
		protected abstract NamedFunction<T> DbFunction { get; set; }
		protected abstract Func<IEnumerable<T>, IEnumerable<T>> Ordering { get; set; }
		public abstract FiltersViewModelBase FiltersViewModel { get; }
		public PausableUpdateList<UserControl> Tiles { get; set; } = new PausableUpdateList<UserControl>();
		private bool _isBlacklisted;
		private SuggestionScorer _suggestionScorer;
		
		public event PropertyChangedEventHandler PropertyChanged;
		public ObservableCollection<NamedFunction<T>> History { get; } = new ObservableCollection<NamedFunction<T>>();
		public CoreSettings CSettings => StaticHelpers.CSettings;

		public CustomFilterBase SelectedFilter
		{
			get => FiltersViewModel.CustomFilter;
			set
			{
				if (FiltersViewModel.CustomFilter == value) return;
				FiltersViewModel.CustomFilter = value;
				Task.Run(() => ChangeFilter(FiltersViewModel.CustomFilter));
			}
		}

		public T[] AllResults { get; private set; }

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

		public SuggestionScorer SuggestionScorer
		{
			get
			{
				if (_suggestionScorer != null) return _suggestionScorer;
				_suggestionScorer = new SuggestionScorer(
					StaticHelpers.CSettings.GetTagScoreDictionary(),
					StaticHelpers.CSettings.GetTraitScoreDictionary(),
					StaticHelpers.LocalDatabase);
				return _suggestionScorer;
			}
		}

		public ListedProducer[] ProducerList => StaticHelpers.LocalDatabase?.Producers?.ToArray();
		public bool BackEnabled => History.ToList().FindIndex(i => i.Selected) > 0;
		public int SelectedFunctionIndex => History.ToList().FindIndex(i => i.Selected);

		protected DatabaseViewModelBase(MainWindowViewModel mainWindowViewModel) => _mainViewModel = mainWindowViewModel;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public async Task Initialize()
		{
			_mainViewModel.StatusText = "Loading VN Database...";
			await Task.Run(() => StaticHelpers.LocalDatabase = new VisualNovelDatabase(StaticHelpers.DatabaseFile, true));
			OnPropertyChanged(nameof(ProducerList));
			_mainViewModel.SetUser(CSettings.UserID);
			_mainViewModel.StatusText = "Opening VNDB Connection...";
			await Task.Run(() =>
			{
				StaticHelpers.Conn = new VndbConnection(SetReplyText, _mainViewModel.VndbAdvancedAction, AskForNonSsl, ChangeConnectionStatus);
				var password = StaticHelpers.LoadPassword();
				StaticHelpers.Conn.Login(password != null
					? new VndbConnection.LoginCredentials(StaticHelpers.ClientName, StaticHelpers.ClientVersion, CSettings.Username, password)
					: new VndbConnection.LoginCredentials(StaticHelpers.ClientName, StaticHelpers.ClientVersion), false);
			});
			_mainViewModel.StatusText = "Loading VN List...";
			await RefreshTiles();
		}

		private void ChangeConnectionStatus(VndbConnection.APIStatus status)
		{
			Dispatcher.CurrentDispatcher.Invoke(() =>
			{
				if (status != VndbConnection.APIStatus.Ready) VndbConnectionStatus = $@"{status} ({StaticHelpers.Conn.ActiveQuery.ActionName})";
				switch (status)
				{
					case VndbConnection.APIStatus.Ready:
						StaticHelpers.Logger.Verbose($"{StaticHelpers.Conn.ActiveQuery?.ActionName} Ended");
						if (StaticHelpers.Conn.ActiveQuery != null) StaticHelpers.Conn.ActiveQuery.Completed = true;
						VndbConnectionStatus = status.ToString();
						VndbConnectionForeground = Brushes.Black;
						VndbConnectionBackground = Brushes.LightGreen;
						break;
					case VndbConnection.APIStatus.Busy:
						StaticHelpers.Logger.Verbose($"{StaticHelpers.Conn.ActiveQuery.ActionName} Started");
						VndbConnectionForeground = new SolidColorBrush(Colors.Red);
						VndbConnectionBackground = new SolidColorBrush(Colors.Khaki);
						break;
					case VndbConnection.APIStatus.Throttled:
						StaticHelpers.Logger.Verbose($"{StaticHelpers.Conn.ActiveQuery.ActionName} Throttled");
						VndbConnectionStatus = $@"{status} ({StaticHelpers.Conn.ActiveQuery.ActionName})";
						VndbConnectionForeground = new SolidColorBrush(Colors.DarkRed);
						VndbConnectionBackground = new SolidColorBrush(Colors.Khaki);
						break;
					case VndbConnection.APIStatus.Error:
						VndbConnectionForeground = new SolidColorBrush(Colors.Black);
						VndbConnectionBackground = new SolidColorBrush(Colors.Red);
						StaticHelpers.Conn.Close();
						break;
					case VndbConnection.APIStatus.Closed:
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

		public async Task RefreshTiles()
		{
			var watch = Stopwatch.StartNew();
			_finalPage = false;
			_currentPage = 1;
			Application.Current.Dispatcher.Invoke(ResolveHistory);
			SetReplyText("Loading Results...", VndbConnection.MessageSeverity.Warning);
			//await db communication
			var results = await Task.Run(() =>
			{
				var items = DbFunction.SelectAndInvoke(StaticHelpers.LocalDatabase);
				OnPropertyChanged(nameof(SelectedFunctionIndex));
				if (!DbFunction.AlwaysIncludeBlacklisted && !IsBlacklisted) items = items.Where(item => !IsBlacklistedFunction(item));
				var filterFunc = FiltersViewModel.PermanentFilter.GetFunction();
				var filteredResults = items.Where(vn => filterFunc(vn));
				AllResults = Ordering(filteredResults).ToArray();
				var firstPage = AllResults.Take(PageSize).ToArray();
				return firstPage;
			});
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() =>
			{
				Tiles.SetRange(results.Select(GetTile));
				OnPropertyChanged(nameof(CSettings));
				OnPropertyChanged(nameof(Tiles));
				OnPropertyChanged(nameof(AllResults));
			});
			watch.Stop();
			SetReplyText($"Loaded results in {watch.Elapsed.ToSeconds()}.", VndbConnection.MessageSeverity.Normal);

			void ResolveHistory()
			{
				var indexOfNew = History.ToList().FindIndex(i => i == DbFunction);
				if (SelectedFunctionIndex == -1) History.Add(DbFunction);
				else if (indexOfNew == -1)
				{
					//clear forward history
					for (int i = History.Count - 1; i > SelectedFunctionIndex; i--) History.RemoveAt(i);
					History.Add(DbFunction);
				}
				OnPropertyChanged(nameof(BackEnabled));
				OnPropertyChanged(nameof(History));
			}
		}
		
		private bool AskForNonSsl()
		{
			var messageResult = System.Windows.Forms.MessageBox.Show(@"Connection to VNDB failed, do you wish to try without SSL?",
				@"Connection Failed", System.Windows.Forms.MessageBoxButtons.YesNo);
			return messageResult == System.Windows.Forms.DialogResult.Yes;
		}

		public void AddPage()
		{
			if (_finalPage) return;
			var overWatch = Stopwatch.StartNew();
			var newPage = AllResults.Skip(_currentPage * PageSize).Take(PageSize).ToList();
			_currentPage++;
			if (newPage.Count < PageSize)
			{
				_finalPage = true;
				if (newPage.Count == 0) return;
			}
			var newTiles = newPage.Select(GetTile).ToList();
			Tiles.AddRange(newTiles);
			overWatch.Stop();
			SetReplyText($"Loaded {newPage.Count} results for page {_currentPage} in {overWatch.Elapsed.ToSeconds()}.", VndbConnection.MessageSeverity.Normal);
			OnPropertyChanged(nameof(Tiles));
			OnPropertyChanged(nameof(AllResults));
		}

		public async Task SearchForItem(string text)
		{
			var vns = StaticHelpers.LocalDatabase.VisualNovels.Where(VisualNovelDatabase.SearchForVN(text)).ToList();
			if (!vns.Any())
			{
				SetReplyText($"Found no results for '{text}'", VndbConnection.MessageSeverity.Normal);
				return;
			}
			DbFunction = new NamedFunction<T>(db => GetAll(db).Where(i=> SearchByText(text)(i)), "Search By VN Name", true);
			await RefreshTiles();
		}

		public async Task ShowTagged(DumpFiles.WrittenTag tag)
		{
			var watch = Stopwatch.StartNew();
			DbFunction = new NamedFunction<T>(
				db => GetAll(db).WithKeyIn(db.Tags.Where(t => tag.AllIDs.Contains(t.TagId)).Select(x => x.VNID).Distinct().ToArray()),
				$"Tag {tag.Name}", false);
			await RefreshTiles();
			StaticHelpers.Logger.ToDebug($@"{nameof(ShowTagged)}: {watch.ElapsedMilliseconds}ms");
		}

		public async Task ChangeFilter(CustomFilterBase item)
		{
			DbFunction = new NamedFunction<T>(db => GetAll(db).Where(i=> item.GetFunction()(i)), item.ToString(), false);
			await RefreshTiles();
		}

		public async Task<bool> ChangeVNStatus(ListedVN vn, HashSet<UserVN.LabelKind> labels) => await StaticHelpers.Conn.ChangeVNStatus(vn, labels);
		public async Task<bool> ChangeVote(ListedVN vn, int? vote) => await StaticHelpers.Conn.ChangeVote(vn, vote);

		public async Task ShowRelatedTitles(T item)
		{
			var vnIds = GetRelatedTitles(item);
			DbFunction = new NamedFunction<T>(db => GetAll(db).WithKeyIn(vnIds),
				$"Related to {item}", true);
			await RefreshTiles();
		}

		public async Task ShowForProducer(string producerName)
		{
			var lowerName = producerName.ToLowerInvariant();
			DbFunction = new NamedFunction<T>(db => GetAll(db).Where(item => GetProducer(item)?.Name.ToLowerInvariant() == lowerName),
				$"Producer {producerName}", true);
			await RefreshTiles();
		}

		public async Task ShowForProducer(ListedProducer producer)
		{
			DbFunction = new NamedFunction<T>(db => GetAll(db).Where(item => GetProducer(item) == producer),
				$"Producer {producer.Name}", true);
			await RefreshTiles();
		}

		public async Task ShowForCharacter(CharacterItem character)
		{
			DbFunction = new NamedFunction<T>(
				db => GetAll(db).WithKeyIn(db.Characters[character.ID].VisualNovels.Select(cvn => cvn.VNId).ToArray()),
				$"Character {character.Name}", true);
			await RefreshTiles();
		}

		protected abstract Func<T, SuggestionScoreObject> GetSuggestion { get; }

		public async Task ShowSuggested()
		{
			var scoredKeys = StaticHelpers.LocalDatabase.VisualNovels.Select(v => v.VNID).ToArray();
			DbFunction = new NamedFunction<T>(db => GetAll(db).WithKeyIn(scoredKeys), "Suggested", false);
			Ordering = lvn => lvn.OrderByDescending(i => GetSuggestion(i)?.Score);
			await RefreshTiles();
		}

		public abstract Task SortByRecommended();

		public abstract Task SortByMyScore();

		public abstract Task SortByRating();

		public abstract Task SortByReleaseDate();

		public abstract Task SortByTitle();

		public async Task ShowAll()
		{
			DbFunction = new NamedFunction<T>(GetAll, "All", false);
			await RefreshTiles();
		}

		public async Task BrowseHistory(NamedFunction<T> item)
		{
			if (DbFunction == item) return;
			DbFunction = item;
			await RefreshTiles();
		}

		public async Task ShowForStaffWithAlias(int aliasId)
		{
			var staff = StaticHelpers.LocalDatabase.StaffAliases[aliasId];
			DbFunction = new NamedFunction<T>(
				db =>
				{
					var aliasIds = db.StaffAliases.Where(c => c.StaffID == staff.StaffID).Select(sa=>sa.AliasID).ToArray();
					var vns = db.VnStaffs.Where(vnStaff => aliasIds.Contains(vnStaff.AliasID)).Select(vnStaff => vnStaff.VNID).ToArray();
					return GetAll(db).WithKeyIn(vns);
				},
				$"Staff {staff}", true);
			await RefreshTiles();
		}

		public async Task ShowForSeiyuu(VnSeiyuu seiyuu)
		{
			var staff = StaticHelpers.LocalDatabase.StaffAliases[seiyuu.AliasID];
			DbFunction = new NamedFunction<T>(
				db =>
				{
					var aliasIds = db.StaffAliases.Where(c => c.StaffID == staff.StaffID).Select(sa => sa.AliasID).ToArray();
					var vns = db.VnSeiyuus.Where(vnSeiyuu => aliasIds.Contains(vnSeiyuu.AliasID)).Select(vnSeiyuu => vnSeiyuu.VNID).ToArray();
					return GetAll(db).WithKeyIn(vns);
				},
				$"Seiyuu {staff}", true);
			await RefreshTiles();
		}

		public abstract int[] GetRelatedTitles(T vn);
	}
}