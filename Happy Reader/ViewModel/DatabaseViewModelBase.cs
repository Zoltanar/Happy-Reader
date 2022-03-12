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
		private bool _orderingByRating;
		private string _replyText;
		private Brush _replyColor;
		private Brush _vndbConnectionBackground;
		private Brush _vndbConnectionForeground;
		private string _vndbConnectionStatus;
		protected readonly MainWindowViewModel MainViewModel;
		public abstract Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> GetAll { get; }
		public abstract IEnumerable<IDataItem<int>> GetAllWithKeyIn(VisualNovelDatabase db, int[] keys);
		protected abstract Func<IDataItem<int>, ListedVN> GetVisualNovel { get; }
		protected abstract Func<IDataItem<int>, double?> GetSuggestion { get; }
		protected abstract Func<IDataItem<int>, string> GetName { get; }
		protected abstract Func<IDataItem<int>, UserControl> GetTile { get; }
		protected abstract NamedFunction DbFunction { get; set; }
		private Func<IEnumerable<IDataItem<int>>, IEnumerable<IDataItem<int>>> _ordering;
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
			private set { _replyText = value; OnPropertyChanged(); }
		}

		public Brush ReplyColor
		{
			get => _replyColor;
			private set { _replyColor = value; OnPropertyChanged(); }
		}

		public Brush VndbConnectionForeground
		{
			get => _vndbConnectionForeground;
			private set { _vndbConnectionForeground = value; OnPropertyChanged(); }
		}

		public Brush VndbConnectionBackground
		{
			get => _vndbConnectionBackground;
			private set { _vndbConnectionBackground = value; OnPropertyChanged(); }
		}

		public string VndbConnectionStatus
		{
			get => _vndbConnectionStatus;
			private set { _vndbConnectionStatus = value; OnPropertyChanged(); }
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

		protected DatabaseViewModelBase(MainWindowViewModel mainWindowViewModel)
		{
			_ordering = items => items.OrderByDescending(i => GetVisualNovel(i)?.ReleaseDate ?? DateTime.MaxValue);
			MainViewModel = mainWindowViewModel;
		}

		[NotifyPropertyChangedInvocator]
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


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

		protected void SetReplyText(string text, VndbConnection.MessageSeverity severity)
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

		protected async Task RefreshTiles()
		{
			var watch = Stopwatch.StartNew();
			_finalPage = false;
			_currentPage = 1;
			Application.Current.Dispatcher.Invoke(ResolveHistory);
			SetReplyText("Loading Results...", VndbConnection.MessageSeverity.Warning);
			//await db communication
			var results = await Task.Run(() =>
			{
				var items = DbFunction.SelectAndInvoke(StaticHelpers.LocalDatabase, this);
				OnPropertyChanged(nameof(SelectedFunctionIndex));
				if (_orderingByRating && StaticMethods.Settings.GuiSettings.ExcludeLowVotesForRatingSort)
				{
					items = items.Where(i => !((GetVisualNovel(i)?.VoteCount ?? 0) < GuiSettings.VotesRequiredForRatingSort));
				}
				var filteredResults = items.Intersect(FiltersViewModel.PermanentFilter.GetAllResults(StaticHelpers.LocalDatabase, GetAll, GetAllWithKeyIn));
				AllResults = _ordering(filteredResults).ToArray();
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

		public void SearchForItem(string text)
		{
			var cf = new CustomFilter($"Text: {text}");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.Name,text));
			SelectedFilter = cf;
		}

		public void ShowTagged(DumpFiles.WrittenTag tag)
		{
			var watch = Stopwatch.StartNew();
			var cf = new CustomFilter($"Tag: {tag.Name}");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.Tags, tag.ID));
			SelectedFilter = cf;
			StaticHelpers.Logger.ToDebug($@"{nameof(ShowTagged)}: {watch.ElapsedMilliseconds}ms");
		}

		public async Task ChangeFilter(CustomFilter item)
		{
			DbFunction = new NamedFunction(item);
			await RefreshTiles();
		}

		public async Task<bool> ChangeVNStatus(ListedVN vn, HashSet<UserVN.LabelKind> labels) => await StaticHelpers.Conn.ChangeVNStatus(vn, labels);
		public async Task<bool> ChangeVote(ListedVN vn, int? vote) => await StaticHelpers.Conn.ChangeVote(vn, vote);

		public void ShowRelatedTitles(IDataItem<int> item)
		{
			var vnIds = GetRelatedTitles(item);
			var cf = new CustomFilter($"Related to: {item}");
			foreach (var vnId in vnIds)
			{
				cf.OrFilters.Add(new GeneralFilter(GeneralFilterType.VNID, vnId));
			}
			cf.SaveOrGroup();
			if (!cf.AndFilters.Any())
			{
				SetReplyText($"No titles related to {item}.", VndbConnection.MessageSeverity.Warning);
				return;
			}
			SelectedFilter = cf;
		}

		public void ShowForProducer(string producerName)
		{
			var producers = StaticHelpers.LocalDatabase.Producers.Where(i => i.Name.Equals(producerName, StringComparison.OrdinalIgnoreCase));
			var cf = new CustomFilter($"Producer Name: {producerName}");
			foreach (var producer in producers)
			{
				cf.OrFilters.Add(new GeneralFilter(GeneralFilterType.Producer, producer.ID));
			}
			cf.SaveOrGroup();
			if (!cf.AndFilters.Any())
			{
				SetReplyText($"No producers found for name {producerName}.", VndbConnection.MessageSeverity.Warning);
				return;
			}
			SelectedFilter = cf;
		}

		public void ShowForProducer(ListedProducer producer)
		{
			var cf = new CustomFilter($"Producer: {producer.Name}");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.Producer, producer.ID));
			SelectedFilter = cf;
		}

		public void ShowForCharacter(CharacterItem character)
		{
			var vnIds = StaticHelpers.LocalDatabase.Characters[character.ID].VisualNovels.Select(cvn => cvn.VNId).ToArray();
			var cf = new CustomFilter($"Character: {character.Name}");
			foreach (var vnid in vnIds)
			{
				cf.OrFilters.Add(new GeneralFilter(GeneralFilterType.VNID, vnid));
			}
			cf.SaveOrGroup();
			if (!cf.AndFilters.Any())
			{
				SetReplyText($"No titles found for character {character.Name}.", VndbConnection.MessageSeverity.Warning);
				return;
			}
			SelectedFilter = cf;
		}

		public void ShowSuggested()
		{
			_ordering = lvn => lvn.OrderByDescending(i => GetSuggestion(i));
			_orderingByRating = false;
			var cf = new CustomFilter("Suggested");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.SuggestionScore, ">0"));
			SelectedFilter = cf;
		}

		public async Task SortBySuggestion()
		{
			_ordering = items => items.OrderByDescending(i => GetSuggestion(i));
			_orderingByRating = false;
			await RefreshTiles();
		}

		public async Task SortByRating()
		{
			_ordering = items => items.OrderByDescending(i => GetVisualNovel(i)?.Rating ?? 0d).ThenByDescending(i => GetVisualNovel(i)?.ReleaseDate ?? DateTime.MinValue);
			_orderingByRating = true;
			await RefreshTiles();
		}
		
		public async Task SortByMyScore()
		{
			_ordering = items => items.OrderByDescending(i => GetVisualNovel(i)?.UserVN?.Vote ?? 0);
			_orderingByRating = false;
			await RefreshTiles();
		}

		public async Task SortByReleaseDate()
		{
			_ordering = items => items.OrderByDescending(i => GetVisualNovel(i).ReleaseDate);
			_orderingByRating = false;
			await RefreshTiles();
		}

		public async Task SortByName()
		{
			_ordering = items => items.OrderBy(i => GetName(i)).ThenByDescending(i => GetVisualNovel(i).ReleaseDate);
			_orderingByRating = false;
			await RefreshTiles();
		}

		public async Task SortByUserAdded()
		{
			_ordering = items => items.OrderByDescending(i => GetVisualNovel(i)?.UserVN?.Added);
			_orderingByRating = true;
			await RefreshTiles();
		}
		
		public async Task SortByUserModified()
		{
			_ordering = items => items.OrderByDescending(i => GetVisualNovel(i)?.UserVN?.LastModified);
			_orderingByRating = true;
			await RefreshTiles();
		}

		public async Task SortByID()
		{
			_ordering = items => items.OrderByDescending(i => i.Key);
			_orderingByRating = false;
			await RefreshTiles();
		}

		public void ShowAll()
		{
			SelectedFilter = new CustomFilter("All");
		}

		public async Task BrowseHistory(NamedFunction function)
		{
			if (DbFunction == function) return;
			DbFunction = function;
			await RefreshTiles();
		}

		public void ShowForStaffWithAlias(string searchHeader, ICollection<int> aliasIds)
		{
			var staffIds = aliasIds.Select(a => StaticHelpers.LocalDatabase.StaffAliases[a].StaffID).ToList();
			var allAliasIds = StaticHelpers.LocalDatabase.StaffAliases.Where(sa => staffIds.Contains(sa.StaffID)).Select(sa => sa.AliasID).Distinct().ToList();
			var cf = new CustomFilter(searchHeader);
			foreach(var id in allAliasIds) cf.OrFilters.Add(new GeneralFilter(GeneralFilterType.Staff, id));
			cf.SaveOrGroup();
			SelectedFilter = cf;
		}

		public void ShowForStaffWithAlias(int aliasId)
		{
			var staff = StaticHelpers.LocalDatabase.StaffAliases[aliasId];
			var cf = new CustomFilter($"Staff: {staff}");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.Staff, aliasId));
			SelectedFilter = cf;
		}

		public void ShowForSeiyuu(VnSeiyuu seiyuu)
		{
			var staff = StaticHelpers.LocalDatabase.StaffAliases[seiyuu.AliasID];
			var cf = new CustomFilter($"Seiyuu: {staff}");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.Seiyuu, seiyuu.AliasID));
			SelectedFilter = cf;
		}

		private int[] GetRelatedTitles(IDataItem<int> item)
		{
			var vn = GetVisualNovel(item);
			if (vn == null) return Array.Empty<int>();
			return vn.GetAllRelations()?.Select(i => i.ID).Concat(new [] {vn.VNID}).Distinct().ToArray() ?? Array.Empty<int>();
		}
	}
}