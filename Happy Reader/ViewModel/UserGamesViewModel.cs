using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View.Tiles;
using JetBrains.Annotations;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.ViewModel
{
	public class UserGamesViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public readonly MainWindowViewModel MainViewModel;

		public ObservableCollection<UserGameTile> UserGameItems { get; } = new();
		
		public UserGamesViewModel(MainWindowViewModel mainViewModel)
		{
			MainViewModel = mainViewModel;
		}

		public async Task Initialize()
		{
			MainViewModel.StatusText = "Loading User Games...";
			await Task.Yield();
			await LoadUserGames(false);

		}

		public async Task LoadUserGames(bool showFileNotFound)
		{
			UserGameItems.Clear();
			IEnumerable<UserGame> orderedGames = null;
			await Task.Run(() =>
			{
				StaticMethods.Data.UserGames.Load();
				foreach (var game in StaticMethods.Data.UserGames.Local)
				{
					if (game.VNID != null)
					{
						game.VN = LocalDatabase.VisualNovels[game.VNID.Value];
						//if game has vn and vn is not already marked as owned, this prevents overwriting CurrentlyOwned with PastOwned,
						//if multiple user games have the same VN but the later one has been deleted.
						if (game.VN != null && game.VN.IsOwned != OwnedStatus.CurrentlyOwned) game.VN.IsOwned = game.FileExists ? OwnedStatus.CurrentlyOwned : OwnedStatus.PastOwned;
					}
				}
				orderedGames = StaticMethods.Data.UserGames.Local.OrderBy(x => x.VNID ?? 0).ToList();
				if (!showFileNotFound) orderedGames = orderedGames.Where(og => og.FileExists);
			});
			foreach (var game in orderedGames) { UserGameItems.Add(new UserGameTile(game)); }
			OnPropertyChanged(nameof(UserGameItems));
		}
		
		public UserGameTile AddGameFile(string file)
		{
			var userGame = StaticMethods.Data.UserGames.FirstOrDefault(x => x.FilePath == file);
			if (userGame != null)
			{
				MainViewModel.StatusText = $"This file has already been added. ({userGame.DisplayName})";
				return null;
			}
			var vn = StaticMethods.ResolveVNForFile(file);
			userGame = new UserGame(file, vn) { Id = StaticMethods.Data.UserGames.Max(x => x.Id) + 1 };
			StaticMethods.Data.UserGames.Add(userGame);
			StaticMethods.Data.SaveChanges();
			MainViewModel.StatusText = vn == null ? "File was added without VN." : $"File was added as {userGame.DisplayName}.";
			return new UserGameTile(userGame);
		}
		
		public void RemoveUserGame(UserGameTile item)
		{
			UserGameItems.Remove(item);
			StaticMethods.Data.UserGames.Remove(item.UserGame);
			StaticMethods.Data.SaveChanges();
			MainViewModel.OnPropertyChanged(nameof(MainViewModel.TestViewModel));
		}

		public void RemoveUserGame(UserGame item)
		{
			var tile = UserGameItems.Single(x => x.UserGame == item);
			RemoveUserGame(tile);
		}

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
