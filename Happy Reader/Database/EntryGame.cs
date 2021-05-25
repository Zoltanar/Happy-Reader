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
			CachedName = cacheName ? GetName(gameId, isUserGame) : null;
		}

		private static string GetName(int? gameId, bool isUserGame)
		{
			if (!gameId.HasValue) return "(No Game)";
			string result;
			if (isUserGame)
			{
				var game = StaticMethods.Data.UserGames[gameId.Value];
				var gameName = game == null ? "Not Found" : game.DisplayName;
				result = $"(UserGame) [{gameId.Value}] {gameName}";
			}
			else
			{
				var game = StaticHelpers.LocalDatabase.VisualNovels[gameId.Value];
				var gameName = game == null ? "Not Found" : game.Title;
				result = $"(VN) [{gameId.Value}] {gameName}";
			}
			return result;
		}

		public override string ToString()
		{
			if (CachedName != null) return CachedName;
			CachedName = GetName(GameId, IsUserGame);
			return CachedName;
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