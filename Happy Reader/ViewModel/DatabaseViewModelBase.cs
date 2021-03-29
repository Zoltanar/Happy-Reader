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
	public abstract class DatabaseViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private const int PageSize = 50;
		private int _currentPage;
		private bool _finalPage;
		private string _replyText;
		private Brush _replyColor;
		private Brush _vndbConnectionBackground;
		private Brush _vndbConnectionForeground;
		private string _vndbConnectionStatus;
		protected readonly MainWindowViewModel MainViewModel;
		protected abstract Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> GetAll { get; }
		protected abstract IEnumerable<IDataItem<int>> GetAllWithKeyIn(VisualNovelDatabase db, int[] keys);
		protected abstract Func<string, Func<IDataItem<int>, bool>> SearchByText { get; }
		protected abstract Func<IDataItem<int>, ListedProducer> GetProducer { get; }
		protected abstract Func<IDataItem<int>, UserControl> GetTile { get; }
		protected abstract NamedFunction DbFunction { get; set; }
		protected abstract Func<IEnumerable<IDataItem<int>>, IEnumerable<IDataItem<int>>> Ordering { get; set; }
		protected Func<IDataItem<int>, bool> Exclude;
		public abstract FiltersViewModel FiltersViewModel { get; }
		public PausableUpdateList<UserControl> Tiles { get; set; } = new();
		private SuggestionScorer _suggestionScorer;
		
		public ObservableCollection<NamedFunction> History { get; } = new();
		public CoreSettings CSettings => StaticHelpers.CSettings;

		public ListedProducer[] ProducerList => StaticHelpers.LocalDatabase?.Producers?.ToArray();
		public bool BackEnabled => History.ToList().FindIndex(i => i.Selected) > 0;
		public int SelectedFunctionIndex => History.ToList().FindIndex(i => i.Selected);

		public CustomFilter SelectedFilter
		{
			get => FiltersViewModel.CustomFilter;
			set
			{
				if (FiltersViewModel.CustomFilter == value) return;
				FiltersViewModel.CustomFilter = value;
				Task.Run(() => ChangeFilter(FiltersViewModel.CustomFilter));
			}
		}

		public IDataItem<int>[] AllResults { get; private set; }

		public string ReplyText
		{
			get => _replyText;
			set { _replyText = value; OnPropertyChanged(); }
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

		protected DatabaseViewModelBase(MainWindowViewModel mainWindowViewModel) => MainViewModel = mainWindowViewModel;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public abstract Task Initialize();

		protected void ChangeConnectionStatus(VndbConnection.APIStatus status)
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
				if (Exclude != null)
				{
					var exclude = Exclude;
					items = items.Where(x=> !exclude(x));
					Exclude = null;
				}
				var filteredResults = items.Intersect(FiltersViewModel.PermanentFilter.GetAllResults(StaticHelpers.LocalDatabase, GetAll, GetAllWithKeyIn));
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
			Func<IDataItem<int>, bool> searchForText = SearchByText(text);
			var results = GetAll(StaticHelpers.LocalDatabase).Where(i => searchForText(i)).Select(t=>t.Key).ToArray();
			if (results.Length == 0)
			{
				SetReplyText($"Found no results for '{text}'", VndbConnection.MessageSeverity.Normal);
				return;
			}
			DbFunction = new NamedFunction(db => GetAllWithKeyIn(db, results), "Text Search");
			await RefreshTiles();
		}

		public async Task ShowTagged(DumpFiles.WrittenTag tag)
		{
			var watch = Stopwatch.StartNew();
			DbFunction = new NamedFunction(
				db => GetAllWithKeyIn(db, db.Tags.Where(t => tag.AllIDs.Contains(t.TagId)).Select(x => x.VNID).Distinct().ToArray()),
				$"Tag {tag.Name}");
			await RefreshTiles();
			StaticHelpers.Logger.ToDebug($@"{nameof(ShowTagged)}: {watch.ElapsedMilliseconds}ms");
		}

		public async Task ChangeFilter(CustomFilter item)
		{
			DbFunction = new NamedFunction(db => item.GetAllResults(db, GetAll, GetAllWithKeyIn), item.ToString());
			await RefreshTiles();
		}

		public async Task<bool> ChangeVNStatus(ListedVN vn, HashSet<UserVN.LabelKind> labels) => await StaticHelpers.Conn.ChangeVNStatus(vn, labels);
		public async Task<bool> ChangeVote(ListedVN vn, int? vote) => await StaticHelpers.Conn.ChangeVote(vn, vote);

		public async Task ShowRelatedTitles(IDataItem<int> item)
		{
			var vnIds = GetRelatedTitles(item);
			DbFunction = new NamedFunction(db => GetAllWithKeyIn(db, vnIds),
				$"Related to {item}");
			await RefreshTiles();
		}

		public async Task ShowForProducer(string producerName)
		{
			var lowerName = producerName.ToLowerInvariant();
			DbFunction = new NamedFunction(db => GetAll(db).Where(item => GetProducer(item)?.Name.ToLowerInvariant() == lowerName),
				$"Producer {producerName}");
			await RefreshTiles();
		}

		public async Task ShowForProducer(ListedProducer producer)
		{
			DbFunction = new NamedFunction(db => GetAll(db).Where(item => GetProducer(item) == producer),
				$"Producer {producer.Name}");
			await RefreshTiles();
		}

		public async Task ShowForCharacter(CharacterItem character)
		{
			DbFunction = new NamedFunction(
				db => GetAllWithKeyIn(db, db.Characters[character.ID].VisualNovels.Select(cvn => cvn.VNId).ToArray()),
				$"Character {character.Name}");
			await RefreshTiles();
		}

		protected abstract Func<IDataItem<int>, double?> GetSuggestion { get; }

		public async Task ShowSuggested()
		{
			var scoredKeys = GetAll(StaticHelpers.LocalDatabase).Where(i=> GetSuggestion(i) >= 0d).Select(i=>i.Key).ToArray();
			DbFunction = new NamedFunction(db => GetAllWithKeyIn(db,scoredKeys), "Suggested");
			Ordering = lvn => lvn.OrderByDescending(i => GetSuggestion(i));
			await RefreshTiles();
		}
		
		public async Task SortBySuggestion()
		{
			Ordering = chars => chars.OrderByDescending(ch => GetSuggestion(ch));
			await RefreshTiles();
		}

		public abstract Task SortByMyScore();

		public abstract Task SortByRating();

		public abstract Task SortByReleaseDate();

		public abstract Task SortByName();

		public async Task SortByID()
		{
			Ordering = chars => chars.OrderByDescending(ch => ch.Key);
			await RefreshTiles();
		}

		public async Task ShowAll()
		{
			DbFunction = new NamedFunction(GetAll, "All");
			await RefreshTiles();
		}

		public async Task BrowseHistory(NamedFunction function)
		{
			if (DbFunction == function) return;
			DbFunction = function;
			await RefreshTiles();
		}

		public virtual async Task ShowForStaffWithAlias(string searchHeader, ICollection<int> aliasIds)
		{
			var staffIds = aliasIds.Select(a=> StaticHelpers.LocalDatabase.StaffAliases[a].StaffID).ToList();
			var allAliasIds = StaticHelpers.LocalDatabase.StaffAliases.Where(sa => staffIds.Contains(sa.StaffID)).Select(sa=>sa.AliasID).ToArray();
			DbFunction = new NamedFunction(
				db =>
				{
					var vns = db.VnStaffs.Where(vnStaff => allAliasIds.Contains(vnStaff.AliasID)).Select(vnStaff => vnStaff.VNID).ToArray();
					return GetAllWithKeyIn(db, vns);
				},
				searchHeader);
			await RefreshTiles();
		}

		public virtual async Task ShowForStaffWithAlias(int aliasId)
		{
			var staff = StaticHelpers.LocalDatabase.StaffAliases[aliasId];
			DbFunction = new NamedFunction(
				db =>
				{
					var aliasIds = db.StaffAliases.Where(c => c.StaffID == staff.StaffID).Select(sa=>sa.AliasID).ToArray();
					var vns = db.VnStaffs.Where(vnStaff => aliasIds.Contains(vnStaff.AliasID)).Select(vnStaff => vnStaff.VNID).ToArray();
					return GetAllWithKeyIn(db, vns);
				},
				$"Staff {staff}");
			await RefreshTiles();
		}

		public async Task ShowForSeiyuu(VnSeiyuu seiyuu)
		{
			var staff = StaticHelpers.LocalDatabase.StaffAliases[seiyuu.AliasID];
			DbFunction = new NamedFunction(
				db =>
				{
					var aliasIds = db.StaffAliases.Where(c => c.StaffID == staff.StaffID).Select(sa => sa.AliasID).ToArray();
					var vns = db.VnSeiyuus.Where(vnSeiyuu => aliasIds.Contains(vnSeiyuu.AliasID)).Select(vnSeiyuu => vnSeiyuu.VNID).ToArray();
					return GetAllWithKeyIn(db, vns);
				},
				$"Seiyuu {staff}");
			await RefreshTiles();
		}

		public abstract int[] GetRelatedTitles(IDataItem<int> item);
	}
}