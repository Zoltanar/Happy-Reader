using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Apps_Core
{
	/// <summary>
	/// Determines which image folders should be synced.
	/// </summary>
	[Flags]
	public enum ImageSyncMode
	{
		None = 0,
		[NotMapped] Characters = 1,
		[NotMapped] Covers = Characters << 1,
		[NotMapped] Screenshots = Covers << 1,
        [NotMapped] ScreenshotThumbnails = Screenshots << 1,
        [NotMapped] CoverThumbnails = ScreenshotThumbnails << 1,
        All = Characters | Covers | Screenshots | ScreenshotThumbnails | CoverThumbnails,
	}
}
