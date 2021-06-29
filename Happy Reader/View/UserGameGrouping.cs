using System.ComponentModel;

namespace Happy_Reader.View
{
	public enum UserGameGrouping
	{
		[Description("By Added")]
		Added = 0,
		[Description("By Producer")]
		Producer = 1,
		[Description("By Release Month")]
		ReleaseMonth = 2,
		[Description("By Name")]
		Name = 3,
		[Description("By Last Played")]
		LastPlayed = 4,
		[Description("By Time Played")]
		TimePlayed = 5,
		[Description("By Tag")]
		Tag = 6,
		[Description("By VN Label")]
		Label = 7,
		[Description("By My Score")]
		Score = 8
	}
}