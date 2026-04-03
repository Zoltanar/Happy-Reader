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
        private bool HasTags { get; }
        private bool HasTraits { get; }
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
			HasTags = Tags.Count > 0;
			HasTraits = Traits.Count > 0;
            _database = database;
		}

		public void SetScore(ListedVN vn, bool useNewConnection, VisualNovelDatabase database)
		{

            var tagScore =  HasTags ? Tags.Sum(sTag => vn.Tags(database).Where(vnTag => vnTag.Score > 0 && sTag.Key.AllIDs.Contains(vnTag.TagId)).Sum(vnTag => sTag.Value * vnTag.Score)) / MaxTagScore : 0d;
			var traitScore = HasTraits ? _database.GetTraitScoreForVn(vn.VNID, IdTraits, useNewConnection) / MaxTraitScore : 0d;
			vn.Suggestion = new SuggestionScoreObject(tagScore, traitScore);
		}
		
		public void SetScore(CharacterItem character, IEnumerable<int> traitIds)
		{
			if (!HasTraits)
			{
				character.TraitScore = 0d;
				return;
            }
			var score = 0d;
			if (traitIds != null)
			{
				foreach (var traitId in traitIds)
				{
					if (IdTraits.TryGetValue(traitId, out var value)) score += value;
				}
			}
			character.TraitScore = score;
		}
	}
}
