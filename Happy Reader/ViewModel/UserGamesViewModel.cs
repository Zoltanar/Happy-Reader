﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.View;
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
        public ComboBoxItem[] UserGameGroupings { get; } = StaticMethods.GetEnumValues(typeof(UserGameGrouping));
        public UserGameGrouping GroupBy
        {
            get => StaticMethods.Settings.GuiSettings.UserGameGrouping;
            set => StaticMethods.Settings.GuiSettings.UserGameGrouping = value;
        }

        public bool ShowNotFound { get; set; }

        public UserGamesViewModel(MainWindowViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
        }

        public async Task Initialize()
        {
            MainViewModel.StatusText = "Loading User Games...";
            await Task.Yield();
            await LoadUserGames();

        }

        private async Task LoadUserGames()
        {
            UserGameItems.Clear();
            IEnumerable<UserGame> orderedGames = null;
            await Task.Run(() =>
            {
                foreach (var game in StaticMethods.Data.UserGames)
                {
                    if (game.VNID != null)
                    {
                        game.VN = LocalDatabase.VisualNovels[game.VNID.Value];
                        //if game has vn and vn is not already marked as owned, this prevents overwriting CurrentlyOwned with PastOwned,
                        //if multiple user games have the same VN but the later one has been deleted.
                        if (game.VN != null && game.VN.IsOwned != OwnedStatus.CurrentlyOwned) game.VN.IsOwned = game.FileExists ? OwnedStatus.CurrentlyOwned : OwnedStatus.PastOwned;
                    }
                }
                orderedGames = StaticMethods.Data.UserGames.OrderBy(x => x.VNID ?? 0).ToList();
                foreach (var entry in StaticMethods.Data.Entries)
                {
                    entry.InitGameId();
                }
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
            userGame = new UserGame(file, vn) { Id = StaticMethods.Data.UserGames.HighestKey + 1 };
            userGame.SaveIconImage();
            StaticMethods.Data.UserGames.Add(userGame, true);
            var entryGame = new EntryGame((int)userGame.Id, true, false);
            if (!EntriesTabViewModel.EntryGames.Contains(entryGame)) EntriesTabViewModel.EntryGames.Add(entryGame);
            MainViewModel.StatusText = vn == null ? "File was added without VN." : $"File was added as {userGame.DisplayName}.";
            return new UserGameTile(userGame);
        }

        public void RemoveUserGame(UserGameTile item)
        {
            UserGameItems.Remove(item);
            StaticMethods.Data.UserGames.Remove(item.UserGame, true);
            var entryGame = new EntryGame((int)item.UserGame.Id, true, false);
            if (EntriesTabViewModel.EntryGames.Contains(entryGame)) EntriesTabViewModel.EntryGames.Remove(entryGame);
            MainViewModel.OnPropertyChanged(nameof(MainViewModel.TestViewModel));
        }

        public void RemoveUserGame(UserGame item)
        {
            var tile = UserGameItems.First(x => x.UserGame == item);
            RemoveUserGame(tile);
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
