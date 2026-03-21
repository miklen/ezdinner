using EzDinner.Core.Aggregates.DishAggregate;
using System.Collections.Generic;

namespace EzDinner.Application.Commands.Dishes
{
    public class DishEnrichmentResult
    {
        public IReadOnlyList<DishRole>? Roles { get; }
        public EffortLevel? EffortLevel { get; }
        public SeasonAffinity? SeasonAffinity { get; }
        public string? Cuisine { get; }

        public DishEnrichmentResult(
            IReadOnlyList<DishRole>? roles,
            EffortLevel? effortLevel,
            SeasonAffinity? seasonAffinity,
            string? cuisine)
        {
            Roles = roles;
            EffortLevel = effortLevel;
            SeasonAffinity = seasonAffinity;
            Cuisine = cuisine;
        }
    }
}
