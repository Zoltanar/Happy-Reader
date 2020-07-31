using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Tiles;
using JetBrains.Annotations;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.ViewModel
{
	public class CharactersTabViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private int _characterTilePage;
		private bool _finalPage;
#if DEBUG
		private const int PageSize = 100;
#else
    private const int PageSize = 100;
#endif
		private Func<VisualNovelDatabase, IEnumerable<CharacterItem>> _dbFunction = x => x.Characters;
		private Func<IEnumerable<CharacterItem>, IEnumerable<CharacterItem>> _ordering = chars => chars.OrderByDescending(x => x.VisualNovelSortingDate);

		public CoreSettings CSettings => StaticHelpers.CSettings;

		public PausableUpdateList<CharacterTile> CharacterTiles { get; set; } = new PausableUpdateList<CharacterTile>();
		public int[] AllVNResults { get; private set; }


		public IEnumerable<ListedProducer> ProducerList => LocalDatabase?.Producers?.AsEnumerable();

		public bool HideTraits { get; set; } = true;

		public async Task Initialize(MainWindowViewModel mainViewModel)
		{
			mainViewModel.StatusText = "Loading Characters...";
			LocalDatabase.SetCharactersAttachedVisualNovels();
			await RefreshCharacterTiles();
		}

		private void ResetOrdering() => _ordering = chars => chars.OrderByDescending(x => x.VisualNovelSortingDate);

		public async Task RefreshCharacterTiles(bool showAll = false, bool resetOrder = true)
		{
			var watch = Stopwatch.StartNew();
			if (resetOrder) ResetOrdering();
			await Task.Run(() =>
			{
				_finalPage = false;
				if (showAll) _dbFunction = x => x.Characters;
				_characterTilePage = 1;
				var characters = _dbFunction.Invoke(LocalDatabase);
				var preFilteredResults = _ordering(characters);
				Func<CharacterItem, bool> filter = c => c.ImageId != null && c.HasFullReleaseDate && c.Gender == "f";
				AllVNResults = preFilteredResults.Where(filter).Select(c => c.ID).ToArray();
				var firstPage = AllVNResults.Take(PageSize).ToArray();
				Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
				Application.Current.Dispatcher.Invoke(() =>
				{
					CharacterTiles.SetRange(firstPage.Select(c => CharacterTile.FromCharacter(LocalDatabase.Characters[c], HideTraits)));
					OnPropertyChanged(nameof(CharacterTiles));
				});
				Logger.ToDebug($"{nameof(RefreshCharacterTiles)} took {watch.Elapsed.ToSeconds()}.");
			});
			Logger.ToDebug($"{nameof(RefreshCharacterTiles)} (2) took {watch.Elapsed.ToSeconds()}.");
		}

		public void AddPage()
		{
			if (_finalPage) return;
			var newPage = AllVNResults.Skip(_characterTilePage * PageSize).Take(PageSize).ToList();
			_characterTilePage++;
			if (newPage.Count < PageSize)
			{
				_finalPage = true;
				if (newPage.Count == 0) return;
			}
			var watch = Stopwatch.StartNew();
			var newTiles = newPage.Select(c => CharacterTile.FromCharacter(LocalDatabase.Characters[c], HideTraits));
			Logger.ToDebug($"[{nameof(AddPage)}] After Creating Tiles: {watch.Elapsed.ToSeconds()}.");
			CharacterTiles.AddRange(newTiles);
			Logger.ToDebug($"[{nameof(AddPage)}] After Adding Tiles: {watch.Elapsed.ToSeconds()}.");
			OnPropertyChanged(nameof(CharacterTiles));
			Logger.ToDebug($"[{nameof(AddPage)}] After OnPropertyChanged: {watch.Elapsed.ToSeconds()}.");
		}

		public async Task Search(string text)
		{
			if (text.StartsWith("id:"))
			{
				var id = text.Substring(3);
				if (!int.TryParse(id, out var idResult)) return;
				var trait = DumpFiles.GetTrait(idResult);
				if (trait == null) return;
				_dbFunction = db => db.GetCharactersWithTrait(trait.ID);
			}
			else
			{
				_dbFunction = db => db.Characters.Where(VisualNovelDatabase.SearchForCharacter(text));
			}
			await RefreshCharacterTiles();
		}

		public async Task ShowForProducer(ListedProducer vnProducer)
		{
			_dbFunction = db => db.Characters.Where(c => c.Producer?.ID == vnProducer.ID);
			await RefreshCharacterTiles();
		}

		public async Task ShowForVisualNovel(CharacterVN visualNovel)
		{
			_dbFunction = db => db.Characters.Where(c => c.CharacterVN?.VNId == visualNovel.VNId);
			await RefreshCharacterTiles();
		}

		public async Task ShowFiltered()
		{
			var trait = DumpFiles.GetTrait(StaticHelpers.CSettings.AlertTraitIDs.First()); //todo use csettings properly
			var filter = new CustomVnFilter();
			filter.AndFilters.Add(new VnFilter(VnFilterType.Blacklisted, null, true));
			filter.AndFilters.Add(new VnFilter(VnFilterType.Label, UserVN.LabelKind.Finished, true));
			filter.AndFilters.Add(new VnFilter(VnFilterType.Label, UserVN.LabelKind.Dropped, true));
			filter.AndFilters.Add(new VnFilter(VnFilterType.Label, UserVN.LabelKind.Playing, true));
			filter.AndFilters.Add(new VnFilter(VnFilterType.ReleaseStatus, ReleaseStatusEnum.Released));
			var function = filter.GetFunction();
			_dbFunction = db => db.GetCharactersWithTrait(trait.ID).Where(c => c.HasVisualNovel && function(c.VisualNovel));
			await RefreshCharacterTiles();
		}

		public async Task ShowSuggested()
		{
			await Task.Delay(0);
			//todo CharacterTab ShowSuggested
		}

		public async Task SortByRecommended()
		{
			await Task.Delay(0);
			//todo CharacterTab SortByRecommended
			/*
			foreach (var listedVN in LocalDatabase.VisualNovels.WithKeyIn(AllVNResults))
			{
				if (listedVN.SuggestionScore == null) SuggestionSorter.GetScore(listedVN);
			}
			_ordering = lvn => lvn.OrderByDescending(vn => vn.SuggestionScore);
			await RefreshCharacterTiles(resetOrder: false);*/
		}

		public async Task SortByID()
		{
			_ordering = chars => chars.OrderByDescending(ch => ch.ID);
			await RefreshCharacterTiles(resetOrder: false);
		}

		public async Task SortByReleaseDate()
		{
			_ordering = chars => chars.OrderByDescending(ch => ch.VisualNovelSortingDate);
			await RefreshCharacterTiles(resetOrder: false);
		}
	}
}
