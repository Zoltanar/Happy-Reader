using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Converters;
using Happy_Reader.View.Tabs;
using Happy_Reader.ViewModel;
using JetBrains.Annotations;

namespace Happy_Reader.View.Tiles
{
	public partial class CharacterTile : UserControl
	{
		private static readonly IValueConverter StaticUserVnToBackgroundConverter = new UserVnToBackgroundConverter();

		private readonly bool _hideTraits;
		private readonly bool _forVnTab;
		private readonly CharacterItem _viewModel;

		private VnMenuItem _vnMenu;
		private bool _loaded;
		
		[NotNull] private MainWindow MainWindow => (MainWindow)Window.GetWindow(this) ?? throw new ArgumentNullException(nameof(MainWindow));
		private MainWindowViewModel MainViewModel => MainWindow.ViewModel;
		private CharactersTabViewModel TabViewModel => this.FindParent<CharactersTab>()?.ViewModel ?? MainViewModel.CharactersViewModel;
		private VnMenuItem VnMenu => _vnMenu ??= new VnMenuItem(_viewModel.VisualNovel);

		public static CharacterTile FromCharacterVN(CharacterVN cvn)
		{
			var character = StaticHelpers.LocalDatabase.Characters[cvn.CharacterId].Clone();
			character.CharacterVN = cvn;
			return new CharacterTile(character, false, true);
		}

		public static CharacterTile FromCharacter(CharacterItem character, bool hideTraits)
		{
			return new CharacterTile(character, hideTraits, false);
		}

		public CharacterTile(CharacterItem character, bool hideTraits, bool forVnTab)
		{
			InitializeComponent();
			_viewModel = character;
			DataContext = _viewModel;
			DescriptionBox.Visibility = hideTraits ? Visibility.Collapsed : Visibility.Visible;
			_hideTraits = hideTraits;
			_forVnTab = forVnTab;
		}
		
		private void CharacterTile_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			VnMenu.TransferItems(VnMenuParent);
			ImageBox.Source = _viewModel.ImageSource == null ? Theme.ImageNotFoundImage : new BitmapImage(new Uri(_viewModel.ImageSource));
			if (_forVnTab)
			{
				VisualNovelNameBox.Visibility = Visibility.Collapsed;
				VisualNovelReleaseBox.Visibility = Visibility.Collapsed;
			}
			else
			{
				//if not for VN tab, we set the background based on user vn status.
				BorderElement.Background = (Brush)StaticUserVnToBackgroundConverter.Convert(_viewModel.VisualNovel?.UserVN, typeof(Brush), null, CultureInfo.CurrentCulture);
			}
			if (_viewModel.ID != 0 && !_hideTraits) SetTraitsList();
			_loaded = true;
		}

		private void SetTraitsList()
		{
			foreach (var group in _viewModel.GetGroupedTraits().OrderBy(g => g.Key))
			{
				TraitsControl.Items.Add(new Run($"{@group.Key}: "));
				foreach (var trait in @group)
				{
					Inline content = new Run(trait?.Name ?? "Not Found");
					if (trait != null && StaticHelpers.CSettings.AlertTraitIDs.Contains(trait.ID)) content = new Bold(content);
					var link = new Hyperlink(content) { Tag = trait };
					link.Click += ShowCharactersWithTrait;
					TraitsControl.Items.Add(link);
				}
			}
		}

		private void ID_OnClick(object sender, RoutedEventArgs e) => Process.Start($"https://vndb.org/c{_viewModel.ID}");

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

		private async void ShowCharactersWithTrait(object sender, EventArgs args)
		{
			var element = (FrameworkContentElement) sender;
			var trait = (DumpFiles.WrittenTrait) element.Tag;
			MainWindow.SelectTab(typeof(CharactersTab));
			await TabViewModel.ShowWithTrait(trait);
		}

		private void OpenVnSubmenu(object sender, RoutedEventArgs e)
		{
			VnMenu.DataContext ??= _viewModel.VisualNovel;
			VnMenu.ContextMenuOpened();
		}
	}
}
