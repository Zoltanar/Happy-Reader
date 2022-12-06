using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Translation;
using Happy_Reader.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Happy_Reader.Model
{
    public class ImportUserGame
    {
        public int GameId { get; set; }
        public UserGame Game { get; set; }
        public List<CachedTranslation> Translations { get; set; }
        public UserGame MatchedGame { get; set; }
        public EntryGame SelectedGame { get; set; }
        public EntryGame[] AllEntryGames  { get;}
        public bool IsSelected { get; set; } = true;

        public ImportUserGame(DACollection<long, UserGame> importGames, IGrouping<int?, CachedTranslation> group, DACollection<long, UserGame> localGames, EntryGame[] allEntryGames)
        {
            GameId = group.Key.Value;
            Game = importGames[group.Key.Value];
            Translations = group.ToList();
            GetMatchedGame(localGames);
            SelectedGame = MatchedGame != null ? new EntryGame((int?)MatchedGame.Id, true, true) : EntryGame.None;
            AllEntryGames = allEntryGames;
        }

        public UserGame GetMatchedGame(DACollection<long, UserGame> localGames)
        {
            if (MatchedGame != null) return MatchedGame;
            MatchedGame = localGames.FirstOrDefault(lg => lg.FilePath == Game.FilePath);
            if (MatchedGame == null) MatchedGame = localGames.FirstOrDefault(lg =>
            Path.GetFileName(Game.FilePath).Equals(Path.GetFileName(lg.FilePath), StringComparison.OrdinalIgnoreCase) &&
            GetParentFolder(Game.FilePath).Equals(GetParentFolder(lg.FilePath)));
            return MatchedGame;
        }

        private string GetParentFolder(string fullPath)
        {
            var parent = Directory.GetParent(fullPath);
            while (parent != null && parent.Parent != null && StaticMethods.Settings.GuiSettings.ExcludedNamesForVNResolve.Contains(parent.Name)) parent = parent.Parent;
            return parent.Name;
        }
    }
}
