using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Happy_Apps_Core.Database;

namespace Happy_Reader
{
	/// <summary>
	/// Describes type of filter
	/// </summary>
	public enum VnFilterType
	{
#pragma warning disable 1591
		[TypeConverter(typeof(LengthFilterEnum))]
		Length = 0,
		//ReleasedBetween = 1,
		[Description("Release Status"), TypeConverter(typeof(ReleaseStatusEnum))]
		ReleaseStatus = 2,
		[TypeConverter(typeof(bool))]
		Blacklisted = 3,
		[TypeConverter(typeof(bool))]
		Voted = 4,
		[Description("By Favorite Producer"), TypeConverter(typeof(bool))]
		ByFavoriteProducer = 5,
		[Description("Label"), TypeConverter(typeof(UserVN.LabelKind))]
		Label = 7,
		[TypeConverter(typeof(string))]
		Language = 8,
		[Description("Original Language"), TypeConverter(typeof(string))]
		OriginalLanguage = 9,
		[TypeConverter(typeof(int))]
		Tags = 10,
		[TypeConverter(typeof(int))]
		Traits = 11,
		[Description("Has Full Date"), TypeConverter(typeof(bool))]
		HasFullDate = 12,
		[Description("Game Owned"), TypeConverter(typeof(OwnedStatus))]
		GameOwned = 13,
		[Description("Is User-related"), TypeConverter(typeof(bool))]
		UserVN = 14,
		[Description("Release Date"), TypeConverter(typeof(string))]
		ReleaseDate = 15,
		[Description("Has Anime"), TypeConverter(typeof(bool))]
		HasAnime = 17,
		[Description("Suggestion Score"), TypeConverter(typeof(string))]
		SuggestionScore = 18,
		[NotMapped]
		Multi = 19,
		[Description("Staff"), TypeConverter(typeof(string))]
		Staff = 20,
#pragma warning restore 1591
	}
}
