using System.ComponentModel;

namespace Happy_Apps_Core.Database
{
	public enum ReleaseStatusEnum
	{
		[Description("Unreleased without date")]
		WithoutReleaseDate = 1,
		[Description("Unreleased with date")]
		WithReleaseDate = 2,
		[Description("Released")]
		Released = 3
	}
}