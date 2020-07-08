using System.ComponentModel;

namespace Happy_Apps_Core.Database
{
	public enum LengthFilterEnum
	{
		[Description("Not Available")]
		NA = 0,
		[Description("<2 Hours")]
		UnderTwoHours = 1,
		[Description("2-10 Hours")]
		TwoToTenHours = 2,
		[Description("10-30 Hours")]
		TenToThirtyHours = 3,
		[Description("30-50 Hours")]
		ThirtyToFiftyHours = 4,
		[Description(">50 Hours")]
		OverFiftyHours = 5,
	}
}