using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Tiles;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.ViewModel
{
	public class CharactersTabViewModel : DatabaseViewModelBase
	{
		public bool HideTraits { get; set; } = true;
		public Action ScrollToTop { get; set; }

		protected override Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> GetAll => db => db.Characters;
		protected override IEnumerable<IDataItem<int>> GetAllWithKeyIn(VisualNovelDatabase db, int[] keys) => db.Characters.WithKeyIn(keys);
		protected override Func<string, Func<IDataItem<int>, bool>> SearchByText => t => i => VisualNovelDatabase.SearchForCharacter(t)((CharacterItem)i);
		protected override Func<IDataItem<int>, ListedProducer> GetProducer { get; } = i => ((CharacterItem)i).VisualNovel?.Producer;
		protected override Func<IDataItem<int>, UserControl> GetTile => i => CharacterTile.FromCharacter((CharacterItem)i, HideTraits);
		protected override NamedFunction DbFunction { get; set; } = new NamedFunction(db => db.Characters, "All");
		protected override Func<IEnumerable<IDataItem<int>>, IEnumerable<IDataItem<int>>> Ordering { get; set; } = chars => chars.OrderByDescending(x => ((CharacterItem)x).VisualNovelSortingDate);
		public override FiltersViewModel FiltersViewModel { get; }

		public CharactersTabViewModel(MainWindowViewModel mainViewModel) : base(mainViewModel)
		{
			FiltersViewModel = new(StaticMethods.AllFilters.CharacterFilters, StaticMethods.AllFilters.CharacterPermanentFilter, this);
		}

		public override async Task Initialize()
		{
			MainViewModel.StatusText = "Loading Characters...";
			OnPropertyChanged(nameof(ProducerList));
			LocalDatabase.SetCharactersAttachedVisualNovels();
			await RefreshTiles();
		}

		private void ResetOrdering() => Ordering = chars => chars.OrderByDescending(x => ((CharacterItem)x).VisualNovelSortingDate);

		//todo implement permanent filter return c.ImageId != null && c.HasFullReleaseDate && c.Gender == "f";

		protected override Func<IDataItem<int>, double?> GetSuggestion { get; } = i => ((CharacterItem)i).TraitScore;

		public async Task ShowForVisualNovel(CharacterVN visualNovel)
		{
			var vn = LocalDatabase.VisualNovels[visualNovel.VNId];
			DbFunction = new NamedFunction(db => db.Characters.Where(c => c.CharacterVN?.VNId == visualNovel.VNId), $"VN: {vn}");
			await RefreshTiles();
		}

		public override async Task ShowForStaffWithAlias(int aliasId)
		{
			var staff = LocalDatabase.StaffAliases[aliasId];
			var aliasIds = LocalDatabase.StaffAliases.Where(c => c.StaffID == staff.StaffID).Select(sa => sa.AliasID).ToList();
			var keys = LocalDatabase.VnStaffs.Where(s => aliasIds.Contains(s.AliasID)).Select(s => s.VNID).Distinct().ToList();
			var characters = LocalDatabase.CharacterVNs.WithKeyIn(keys).Select(cvn => cvn.CharacterId).ToArray();
			DbFunction = new NamedFunction(db => db.Characters.WithKeyIn(characters), $"Staff: {staff}");
			await RefreshTiles();
		}

		public override int[] GetRelatedTitles(IDataItem<int> item)
		{
			return ((CharacterItem)item).VisualNovel?.AllRelations?.Select(i => i.ID).ToArray() ?? Array.Empty<int>();
		}

		public async Task ShowForSeiyuuWithAlias(int aliasId)
		{
			var staff = LocalDatabase.StaffAliases[aliasId];
			var aliasIds = LocalDatabase.StaffAliases.Where(c => c.StaffID == staff.StaffID).Select(sa => sa.AliasID).ToArray();
			var keys = LocalDatabase.VnSeiyuus.Where(s => aliasIds.Contains(s.AliasID)).Select(s => s.CharacterID).Distinct().ToArray();
			DbFunction = new NamedFunction(db => db.Characters.WithKeyIn(keys), $"Seiyuu: {staff}");
			await RefreshTiles();
		}

		public async Task ShowWithTrait(DumpFiles.WrittenTrait trait)
		{
			DbFunction = new NamedFunction(db =>
			{
				return db.Characters.Where(c => c.DbTraits.Any(t => trait.AllIDs.Contains(t.TraitId)));
			}, $"Trait: {trait}");
			await RefreshTiles();
		}

		public override async Task SortByMyScore()
		{
			Ordering = chars => chars.OrderByDescending(x => ((CharacterItem)x).VisualNovel?.UserVN?.Vote ?? 0);
			await RefreshTiles();
		}

		public override async Task SortByRating()
		{
			Ordering = chars => chars.OrderByDescending(x => ((CharacterItem)x).VisualNovel?.Rating ?? 0d);
			if (StaticMethods.Settings.GuiSettings.ExcludeLowVotesForRatingSort) Exclude = x => (((CharacterItem)x).VisualNovel?.VoteCount ?? 0) < 10;
			await RefreshTiles();
		}


		public override async Task SortByReleaseDate()
		{
			Ordering = chars => chars.OrderByDescending(x => ((CharacterItem)x).VisualNovelSortingDate);
			await RefreshTiles();
		}

		public override async Task SortByName()
		{
			Ordering = chars => chars.OrderByDescending(x => ((CharacterItem)x).Name);
			await RefreshTiles();
		}
	}
}
