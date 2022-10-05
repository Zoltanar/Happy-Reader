using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Converters;
using Happy_Reader.ViewModel;

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

		private VnMenuItem VnMenu => _vnMenu ??= new VnMenuItem(_viewModel.VisualNovel);

		public static CharacterTile FromCharacterVN(CharacterVN cvn)
		{
			var character = StaticHelpers.LocalDatabase.Characters[cvn.CharacterId].Clone();
			character.CharacterVN = cvn;
			return new CharacterTile(character, false, true);
		}

		public static CharacterTile FromCharacter(CharacterItem character, bool hideTraits)
		{
			return new(character, hideTraits, false);
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
                ProducerBox.Visibility = Visibility.Collapsed;
                VisualNovelReleaseBox.Visibility = Visibility.Collapsed;
			}
			else
			{
				//if not for VN tab, we set the background based on user vn status.
				var binding = BorderElement.SetBinding(Border.BackgroundProperty,
					new Binding($"{nameof(CharacterItem.VisualNovel)}.{nameof(CharacterItem.VisualNovel.UserVN)}")
					{ Converter = StaticUserVnToBackgroundConverter });
				binding.UpdateTarget();
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
					var tooltip = trait != null ? TitleDescriptionConverter.Instance.Convert(trait.Description, typeof(string), null, CultureInfo.CurrentCulture) : null;
					var link = new Hyperlink(content) { Tag = trait, ToolTip = tooltip };
					if (trait != null) link.Click += ShowCharactersWithTrait;
					TraitsControl.Items.Add(link);
				}
			}
		}

		private void ID_OnClick(object sender, RoutedEventArgs e) => Process.Start($"https://vndb.org/c{_viewModel.ID}");

		private void ShowCharactersByProducer(object sender, RoutedEventArgs e)
		{
			if (_viewModel.Producer == null) throw new InvalidOperationException("Character does not have producer.");
			StaticMethods.MainWindow.SelectTab(typeof(CharactersTabViewModel));
			StaticMethods.MainWindow.ViewModel.CharactersViewModel.ShowForProducer(_viewModel.Producer);
		}

		private void ShowCharactersForVn(object sender, RoutedEventArgs e)
		{
			if (_viewModel.CharacterVN == null) throw new InvalidOperationException("Character does not have a visual novel.");
			StaticMethods.MainWindow.SelectTab(typeof(CharactersTabViewModel));
			StaticMethods.MainWindow.ViewModel.CharactersViewModel.ShowForVisualNovel(_viewModel.CharacterVN);
		}

		private void ShowVisualNovelsByProducer(object sender, RoutedEventArgs e)
		{
			if (_viewModel.Producer == null) throw new InvalidOperationException("Character does not have producer.");
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowForProducer(_viewModel.Producer);
		}

		private void ShowVisualNovelsForCharacter(object sender, RoutedEventArgs e)
		{
			if (_viewModel.CharacterVN == null) throw new InvalidOperationException("Character does not have a visual novel.");
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowForCharacter(_viewModel);
		}

		private void ShowVisualNovelsForSeiyuu(object sender, RoutedEventArgs e)
		{
			if (_viewModel.Seiyuu == null) throw new InvalidOperationException("Character does not have a Seiyuu.");
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowForSeiyuu(_viewModel.Seiyuu);
		}

		private void ShowCharactersForSeiyuu(object sender, RoutedEventArgs e)
		{
			if (_viewModel.Seiyuu == null) throw new InvalidOperationException("Character does not have a Seiyuu.");
			StaticMethods.MainWindow.SelectTab(typeof(CharactersTabViewModel));
			StaticMethods.MainWindow.ViewModel.CharactersViewModel.ShowForSeiyuuWithAlias(_viewModel.Seiyuu.AliasID);
		}

		private void ShowCharactersWithTrait(object sender, EventArgs args)
		{
			var element = (FrameworkContentElement)sender;
			var trait = (DumpFiles.WrittenTrait)element.Tag;
			StaticMethods.MainWindow.SelectTab(typeof(CharactersTabViewModel));
			StaticMethods.MainWindow.ViewModel.CharactersViewModel.ShowWithTrait(trait);
		}

		private void OpenVnSubmenu(object sender, RoutedEventArgs e)
		{
			VnMenu.DataContext ??= _viewModel.VisualNovel;
			VnMenu.ContextMenuOpened(false);
		}

		private void OnDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (_viewModel.VisualNovel == null) return;
			StaticMethods.MainWindow.OpenVNPanel(_viewModel.VisualNovel);
		}
	}
}
