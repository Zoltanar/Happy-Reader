namespace Happy_Apps_Core
{
	public class SuggestionScoreObject
	{
		public double Score { get; }

		public double TagScore { get; }

		public double TraitScore { get; }
		public string Detail { get; }

		public SuggestionScoreObject(double? tagScore, double? traitScore)
		{
			TagScore = tagScore ?? 0;
			TraitScore = traitScore ?? 0;
			Score = TagScore + TraitScore;
			const string intFormat = "0.##";
			if (TagScore == default && TraitScore == default) Detail = 0.ToString(intFormat);
			else if (TraitScore == default) Detail = $"T:{TagScore.ToString(intFormat)}";
			else if (TagScore == default) Detail = $"C:{TraitScore.ToString(intFormat)}";
			else Detail = $"{TagScore.ToString(intFormat)} {TraitScore.ToString(intFormat)}";
		}
	}
}