using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Reader
{
	public enum CharacterFilterType
	{
		[Description("Suggestion Score"), TypeConverter(typeof(string))]
		TraitScore = 1,
		[TypeConverter(typeof(string))]
		Traits = 2,
		[NotMapped]
		Multi = 3,
		[TypeConverter(typeof(string))]
		Gender = 4,
		[Description("Has Image"), TypeConverter(typeof(bool))]
		HasImage = 5,
		[Description("Is User-related"), TypeConverter(typeof(bool))]
		UserVN = 14,
		[Description("Has Full Date"), TypeConverter(typeof(bool))]
		HasFullDate = 12,
	}
}