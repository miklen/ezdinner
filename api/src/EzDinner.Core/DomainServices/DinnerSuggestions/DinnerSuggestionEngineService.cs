using System.Collections.Generic;
using System.Linq;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class DinnerSuggestionEngineService
    {
        private readonly IEnumerable<IScoringRule> _rules;

        public DinnerSuggestionEngineService(IEnumerable<IScoringRule> rules)
        {
            _rules = rules;
        }

        public IReadOnlyList<DishScoreValueObject> Rank(IEnumerable<DishCandidateValueObject> candidates, SuggestionContextValueObject context)
        {
            return candidates
                .Select(c => new DishScoreValueObject(c.DishId, c.DishName, _rules.Sum(r => r.Score(c, context)), c.Rating, c.DaysSinceLast))
                .OrderByDescending(s => s.TotalScore)
                .ToList();
        }
    }
}
