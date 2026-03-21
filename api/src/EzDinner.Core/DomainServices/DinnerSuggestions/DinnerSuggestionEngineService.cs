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
            var explainableRules = _rules.OfType<IExplainableScoringRule>().ToList();

            return candidates
                .Select(c =>
                {
                    var score = _rules.Sum(r => r.Score(c, context));
                    var reasons = explainableRules
                        .Select(r => r.Explain(c, context))
                        .Where(r => r is not null)
                        .Select(r => r!)
                        .ToList();
                    return new DishScoreValueObject(c.DishId, c.DishName, score, c.Rating, c.DaysSinceLast, reasons);
                })
                .OrderByDescending(s => s.TotalScore)
                .ToList();
        }
    }
}
