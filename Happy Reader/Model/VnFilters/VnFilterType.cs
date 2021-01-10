using System.ComponentModel;

namespace Happy_Reader
{
	/// <summary>
	/// Describes type of filter
	/// </summary>
	public enum VnFilterType
	{
#pragma warning disable 1591
		Length = 0,
		//ReleasedBetween = 1,
		[Description("Release Status")]
		ReleaseStatus = 2,
		Blacklisted = 3,
		Voted = 4,
		[Description("By Favorite Producer")]
		ByFavoriteProducer = 5,
		[Description("Label")]
		Label = 7,
		Language = 8,
		[Description("Original Language")]
		OriginalLanguage = 9,
		Tags = 10,
		Traits = 11,
		[Description("Has Full Date")]
		HasFullDate = 12,
		[Description("Game Owned")]
		GameOwned = 13,
		[Description("Is User-related")]
		UserVN = 14,
		[Description("Release Date")]
		ReleaseDate = 15,
		[Description("Has Anime")]
		HasAnime = 17,
		[Description("Suggestion Score")]
		SuggestionScore = 18,
		Multi = 19
#pragma warning restore 1591
	}
}
