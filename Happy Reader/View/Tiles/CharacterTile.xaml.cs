using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Tabs;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View.Tiles
{
	public partial class CharacterTile : UserControl
	{
		private readonly CharacterItem _viewModel;

		[NotNull]
		private MainWindow MainWindow => (MainWindow)Window.GetWindow(this) ?? throw new ArgumentNullException(nameof(MainWindow));

		private MainWindowViewModel MainViewModel => MainWindow.ViewModel;

		private CharactersTabViewModel TabViewModel => this.FindParent<CharactersTab>()?.ViewModel
																									 ?? MainViewModel.CharactersViewModel;

		private readonly bool _hideTraits;

		public CharacterTile(CharacterItem character, bool hideTraits)
		{
			InitializeComponent();
			_viewModel = character;
			DataContext = _viewModel;
			DescriptionBox.Visibility = hideTraits ? Visibility.Collapsed : Visibility.Visible;
			_hideTraits = hideTraits;
		}

		private bool _loaded;

		private void CharacterTile_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			if (_viewModel.ImageSource != DependencyProperty.UnsetValue) ImageBox.Source = new BitmapImage(new Uri((string)_viewModel.ImageSource));
			if (_hideTraits) return;
			var linkList = new List<Inline>();
			if (_viewModel.ID != 0)
			{
				var traits = _viewModel.DbTraits.ToList();
				if (traits.Count > 0)
				{
					SetTraitsList(traits, linkList);
				}
			}
			TraitsControl.ItemsSource = linkList;
			_loaded = true;
		}

		private void SetTraitsList(List<DbTrait> traits, List<Inline> linkList)
		{
			var rootTraits = Enum.GetValues(typeof(DumpFiles.RootTrait)).Cast<int>().ToList();
			var groups = traits.Where(t => rootTraits.Contains(DumpFiles.GetTrait(t.TraitId).TopmostParent))
				.Select(trait => DumpFiles.GetTrait(trait.TraitId)).GroupBy(x => x?.TopmostParentName ?? "Not Found");
			foreach (var group in groups.OrderBy(g => g.Key))
			{
				linkList.Add(new Run($"{@group.Key}: "));
				foreach (var trait in @group)
				{
					Inline content = new Run(trait?.Name ?? "Not Found");
					if (trait != null && StaticHelpers.CSettings.AlertTraitIDs.Contains(trait.ID)) content = new Bold(content);
					var link = new Hyperlink(content) { Tag = trait };
					linkList.Add(link);
				}
			}
		}

		public static CharacterTile FromCharacterVN(CharacterVN cvn)
		{
			var character = StaticHelpers.LocalDatabase.Characters.First(c => c.ID == cvn.CharacterId).Clone();
			character.CharacterVN = cvn;
			return new CharacterTile(character, false);
		}

		public static CharacterTile FromCharacter(CharacterItem character, bool hideTraits)
		{
			return new CharacterTile(character, hideTraits);
		}

		private void ID_OnClick(object sender, RoutedEventArgs e)
		{
			Process.Start($"https://vndb.org/c{_viewModel.ID}");
		}

		private async void ShowCharactersByProducer(object sender, RoutedEventArgs e)
		{
			if (_viewModel.Producer == null) throw new InvalidOperationException("Character does not have producer.");
			MainWindow.SelectTab(typeof(CharactersTab));
			await TabViewModel.ShowForProducer(_viewModel.Producer);
		}

		private async void ShowCharactersForVn(object sender, RoutedEventArgs e)
		{
			if (_viewModel.CharacterVN == null) throw new InvalidOperationException("Character does not have a visual novel.");
			MainWindow.SelectTab(typeof(CharactersTab));
			await TabViewModel.ShowForVisualNovel(_viewModel.CharacterVN);
		}

		private async void ShowVisualNovelsByProducer(object sender, RoutedEventArgs e)
		{
			if (_viewModel.Producer == null) throw new InvalidOperationException("Character does not have producer.");
			MainWindow.SelectTab(typeof(DatabaseTab));
			await MainViewModel.DatabaseViewModel.ShowForProducer(_viewModel.Producer);
		}

		private async void ShowVisualNovelsForCharacter(object sender, RoutedEventArgs e)
		{
			if (_viewModel.CharacterVN == null) throw new InvalidOperationException("Character does not have a visual novel.");
			MainWindow.SelectTab(typeof(DatabaseTab));
			await MainViewModel.DatabaseViewModel.ShowForCharacter(_viewModel);
		}

		private async void ShowVisualNovelsForSeiyuu(object sender, RoutedEventArgs e)
		{
			if (_viewModel.Seiyuu == null) throw new InvalidOperationException("Character does not have a Seiyuu.");
			MainWindow.SelectTab(typeof(DatabaseTab));
			await MainViewModel.DatabaseViewModel.ShowForSeiyuu(_viewModel.Seiyuu);
		}

		private async void ShowCharactersForSeiyuu(object sender, RoutedEventArgs e)
		{
			if (_viewModel.Seiyuu == null) throw new InvalidOperationException("Character does not have a Seiyuu.");
			MainWindow.SelectTab(typeof(CharactersTab));
			await TabViewModel.ShowForSeiyuuWithAlias(_viewModel.Seiyuu.AliasID);
		}
	}
}
