using Happy_Apps_Core;

namespace Happy_Reader.Database
{
	public class EntryGame
	{
		public static EntryGame None {get;} = new(null, false, false);
		public int? GameId { get; }
		public bool IsUserGame { get; }
		private string CachedName { get; set; }

		public EntryGame(int? gameId, bool isUserGame, bool cacheName)
		{
			GameId = gameId;
			IsUserGame = isUserGame;
			CachedName = cacheName ? GetName() : null;
		}

        private string GetName()
        {
            if (CachedName != null) return CachedName;
			if (!GameId.HasValue) return "(No Game)";
			string result;
			if (IsUserGame)
			{
				var game = StaticMethods.Data.UserGames[GameId.Value];
				var gameName = game == null ? "Not Found" : game.DisplayName;
				result = $"(UserGame) [{GameId.Value}] {gameName}";
			}
			else
			{
				var game = StaticHelpers.LocalDatabase.VisualNovels[GameId.Value];
				var gameName = game == null ? "Not Found" : StaticHelpers.TruncateString30(game.Title);
				result = $"(VN) [{GameId.Value}] {gameName}";
			}
            CachedName = result;
			return result;
		}

        public string GetGameNameOnly()
		{
            if (!GameId.HasValue) return "(No Game)";
            if (IsUserGame)
            {
                var game = StaticMethods.Data.UserGames[GameId.Value];
                return game == null ? "Not Found" : game.DisplayName;
            }
            else
            {
                var game = StaticHelpers.LocalDatabase.VisualNovels[GameId.Value];
                return game == null ? "Not Found" : StaticHelpers.TruncateString30(game.Title);
            }
		}

		public override string ToString()
		{
			return GetName();
		}

		public bool Equals(EntryGame other)
		{
			return GameId == other.GameId && IsUserGame == other.IsUserGame;
		}

		public override bool Equals(object obj)
		{
			return obj is EntryGame other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (GameId.GetHashCode() * 397) ^ IsUserGame.GetHashCode();
			}
		}
	}
}