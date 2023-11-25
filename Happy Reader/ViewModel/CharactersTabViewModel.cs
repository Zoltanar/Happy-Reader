using System;
using System.Collections.Generic;
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
		private readonly bool HideTraits = true; //todo make this a setting?

		public override Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> GetAll => db => db.Characters;
		public override IEnumerable<IDataItem<int>> GetAllWithKeyIn(VisualNovelDatabase db, int[] keys) => db.Characters.WithKeyIn(keys);
		protected override Func<IDataItem<int>, ListedVN> GetVisualNovel => i => ((CharacterItem)i).VisualNovel;
		protected override Func<IDataItem<int>, string> GetName => i => ((CharacterItem)i).Name;
		protected override Func<IDataItem<int>, UserControl> GetTile => i => CharacterTile.FromCharacter((CharacterItem)i, HideTraits);
		protected override NamedFunction DbFunction { get; set; } = new(new CustomFilter("All"));
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
            SelectedFilterIndex = 0;
            //await RefreshTiles();
		}

		protected override Func<IDataItem<int>, double?> GetSuggestion { get; } = i => ((CharacterItem)i).TraitScore;

		public void ShowForVisualNovel(CharacterVN visualNovel)
		{
			var vn = LocalDatabase.VisualNovels[visualNovel.VNId];
			var cf = new CustomFilter($"VN: {TruncateString15(vn.Title)}");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.VNID, vn.VNID));
			ActiveFilter = cf;
		}

		public void ShowForSeiyuuWithAlias(int aliasId)
		{
			var staff = LocalDatabase.StaffAliases[aliasId];
			var cf = new CustomFilter($"Seiyuu: {staff}");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.Seiyuu, aliasId));
			ActiveFilter = cf;
		}

		public void ShowWithTrait(DumpFiles.WrittenTrait trait)
		{
			var cf = new CustomFilter($"Trait: {trait}");
			cf.AndFilters.Add(new GeneralFilter(GeneralFilterType.Traits, trait.ID));
			ActiveFilter = cf;
		}
	}
}
