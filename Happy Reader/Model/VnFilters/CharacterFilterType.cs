using System.ComponentModel;

namespace Happy_Reader
{
	public enum CharacterFilterType
	{
		[Description("Suggestion Score")]
		TraitScore = 1,
		Traits = 2,
		Multi = 3,
		Gender = 4,
		[Description("Has Image")]
		HasImage = 5,
		[Description("Is User-related")]
		UserVN = 14,
		[Description("Has Full Date")]
		HasFullDate = 12,
	}
}