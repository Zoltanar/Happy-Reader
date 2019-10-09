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
		[Description("Wishlist Status")]
		WishlistStatus = 6,
		[Description("Userlist Status")]
		UserlistStatus = 7,
		Language = 8,
		[Description("Original Language")]
		OriginalLanguage = 9,
		Tags = 10,
		Traits = 11,
		HasFullDate = 12
#pragma warning restore 1591
	}
}
