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
		private string _replyText;
		private Brush _replyColor;
		private const int PageSize = 100;
		private Func<VisualNovelDatabase, IEnumerable<CharacterItem>> _dbFunction = x => x.Characters;
		private Func<IEnumerable<CharacterItem>, IEnumerable<CharacterItem>> _ordering = chars => chars.OrderByDescending(x => x.VisualNovelSortingDate);

		public PausableUpdateList<CharacterTile> CharacterTiles { get; set; } = new PausableUpdateList<CharacterTile>();
		public int[] AllResults { get; private set; }

		public IEnumerable<ListedProducer> ProducerList => LocalDatabase?.Producers?.AsEnumerable();

		public bool HideTraits { get; set; } = true; 
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
		public Action ScrollToTop { get; set; }

		public async Task Initialize(MainWindowViewModel mainViewModel)
		{
			mainViewModel.StatusText = "Loading Characters...";
			LocalDatabase.SetCharactersAttachedVisualNovels();
			await RefreshCharacterTiles();
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

		private void ResetOrdering() => _ordering = chars => chars.OrderByDescending(x => x.VisualNovelSortingDate);

		public async Task RefreshCharacterTiles(bool showAll = false, bool resetOrder = true)
		{
			var watch = Stopwatch.StartNew();
			if (resetOrder) ResetOrdering();
			ScrollToTop();
			await Task.Run(() =>
		 {
			 _finalPage = false;
			 if (showAll) _dbFunction = x => x.Characters;
			 _characterTilePage = 1;
			 var characters = _dbFunction.Invoke(LocalDatabase);
			 var preFilteredResults = _ordering(characters);
			 Func<CharacterItem, bool> filter = c => c.ImageId != null && c.HasFullReleaseDate && c.Gender == "f";
			 AllResults = preFilteredResults.Where(filter).Select(c => c.ID).ToArray();
			 var firstPage = AllResults.Take(PageSize).ToArray();
			 Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			 Application.Current.Dispatcher.Invoke(() =>
			 {
				 CharacterTiles.SetRange(firstPage.Select(c => CharacterTile.FromCharacter(LocalDatabase.Characters[c], HideTraits)));
				 OnPropertyChanged(nameof(CharacterTiles));
				 OnPropertyChanged(nameof(AllResults));
			 });
		 });
			SetReplyText("", VndbConnection.MessageSeverity.Normal);
			Logger.ToDebug($"{nameof(RefreshCharacterTiles)} took {watch.Elapsed.ToSeconds()}.");
		}

		public void AddPage()
		{
			if (_finalPage) return;
			var newPage = AllResults.Skip(_characterTilePage * PageSize).Take(PageSize).ToList();
			_characterTilePage++;
			if (newPage.Count < PageSize)
			{
				_finalPage = true;
				if (newPage.Count == 0) return;
			}
			var watch = Stopwatch.StartNew();
			var newTiles = newPage.Select(c => CharacterTile.FromCharacter(LocalDatabase.Characters[c], HideTraits)).ToList();
			Logger.ToDebug($"[{nameof(AddPage)}] Creating character tiles: {watch.Elapsed.ToSeconds()}.");
			CharacterTiles.AddRange(newTiles);
			OnPropertyChanged(nameof(CharacterTiles));
		}

		public async Task Search(string text)
		{
			var characters = LocalDatabase.Characters.Where(VisualNovelDatabase.SearchForCharacter(text)).Select(c => c.ID).ToArray();
			if (!characters.Any())
			{
				SetReplyText($"Found no results for '{text}'", VndbConnection.MessageSeverity.Normal);
				return;
			}
			_dbFunction = db => db.Characters.WithKeyIn(characters);
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
			_dbFunction = db => db.GetCharactersWithTrait(trait.ID).Where(c => c.VisualNovel != null && function(c.VisualNovel));
			await RefreshCharacterTiles();
		}

		public async Task ShowForStaffWithAlias(int aliasId)
		{
			_dbFunction = db =>
			{
				var staff = db.StaffAliases[aliasId];
				var aliasIds = db.StaffAliases.Where(c => c.StaffID == staff.StaffID).Select(sa => sa.AliasID).ToList();
				var keys = db.VnStaffs.Where(s => aliasIds.Contains(s.AliasID)).Select(s => s.VNID).Distinct().ToList();
				var characters = db.CharacterVNs.WithKeyIn(keys).Select(cvn => cvn.CharacterId).ToArray();
				return db.Characters.WithKeyIn(characters);
			};
			await RefreshCharacterTiles();
		}
		public async Task ShowForSeiyuuWithAlias(int aliasId)
		{
			_dbFunction = db =>
			{
				var staff = db.StaffAliases[aliasId];
				var aliasIds = db.StaffAliases.Where(c => c.StaffID == staff.StaffID).Select(sa => sa.AliasID).ToArray();
				var keys = db.VnSeiyuus.Where(s => aliasIds.Contains(s.AliasID)).Select(s => s.CharacterID).Distinct().ToArray();
				return db.Characters.WithKeyIn(keys);
			};
			await RefreshCharacterTiles();
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
