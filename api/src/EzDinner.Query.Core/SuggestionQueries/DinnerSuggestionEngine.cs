using System.Collections.Generic;
using System.Linq;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class DinnerSuggestionEngine
    {
        private readonly IEnumerable<IScoringRule> _rules;

        public DinnerSuggestionEngine(IEnumerable<IScoringRule> rules)
        {
            _rules = rules;
        }

        public IReadOnlyList<DishScore> Rank(IEnumerable<DishCandidate> candidates, SuggestionContext context)
        {
            return candidates
                .Select(c => new DishScore(c.DishId, c.DishName, _rules.Sum(r => r.Score(c, context)), c.Rating, c.DaysSinceLast))
                .OrderByDescending(s => s.TotalScore)
                .ToList();
        }
    }
}
