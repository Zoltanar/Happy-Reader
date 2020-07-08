using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;

namespace Happy_Apps_Core
{
	public class SuggestionScorer
	{
		/// <summary>
		/// Key is tag, value is power of tag.
		/// </summary>
		public readonly Dictionary<DumpFiles.WrittenTag, double> Tags;
		/// <summary>
		/// Key is trait, value is power of trait.
		/// </summary>
		public readonly Dictionary<DumpFiles.WrittenTrait, double> Traits;
		/// <summary>
		/// Key is trait id, value is power of trait. (contains sub traits)
		/// </summary>
		public readonly Dictionary<int, double> IdTraits;
		/// <summary>
		/// Ids of all tags and subtags.
		/// </summary>
		public readonly HashSet<int> IdTags;
		public double MaxTagScore { get; }
		public double MaxTraitScore { get; }
		private readonly VisualNovelDatabase _database;

		public SuggestionScorer(
			Dictionary<DumpFiles.WrittenTag, double> tagScores,
			Dictionary<DumpFiles.WrittenTrait, double> traitScores,
			VisualNovelDatabase database)
		{
			Tags = tagScores;
			Traits = traitScores;
			var idTraits = new Dictionary<int,double>();
			foreach (var pair in traitScores)
			{
				foreach (var id in pair.Key.AllIDs)
				{
					idTraits[id] = pair.Value;
				}
			}
			IdTraits = idTraits;
			IdTags = Tags.SelectMany(t => t.Key.AllIDs).ToHashSet();
			MaxTagScore = Tags.Sum(pair => pair.Value);
			MaxTraitScore = Traits.Sum(pair => pair.Value);
			_database = database;
		}

		public double GetScore(ListedVN vn, bool useNewConnection)
		{
			var tagScore = Tags.Sum(sTag => vn.Tags.Where(vnTag => sTag.Key.AllIDs.Contains(vnTag.TagId)).Sum(vnTag => sTag.Value * vnTag.Score));
			var traitScore = _database.GetTraitScoreForVn(vn.VNID, IdTraits, useNewConnection);
			vn.Suggestion = new SuggestionScoreObject(tagScore/ MaxTagScore, traitScore / MaxTraitScore);
			return vn.Suggestion.Score;
		}
	}
}
