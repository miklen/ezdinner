using EzDinner.Core.Aggregates.DishAggregate;
using System;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class DishCandidateValueObject
    {
        public Guid DishId { get; }
        public string DishName { get; }
        public double Rating { get; }
        public int DaysSinceLast { get; }
        public double TypicalFrequencyDays { get; }
        public double LeftoverFrequencyRatio { get; }
        public EffortLevel? EffortLevel { get; }
        public SeasonAffinity? SeasonAffinity { get; }

        public DishCandidateValueObject(
            Guid dishId,
            string dishName,
            double rating,
            int daysSinceLast,
            double typicalFrequencyDays,
            double leftoverFrequencyRatio,
            EffortLevel? effortLevel = null,
            SeasonAffinity? seasonAffinity = null)
        {
            DishId = dishId;
            DishName = dishName;
            Rating = rating;
            DaysSinceLast = daysSinceLast;
            TypicalFrequencyDays = typicalFrequencyDays;
            LeftoverFrequencyRatio = leftoverFrequencyRatio;
            EffortLevel = effortLevel;
            SeasonAffinity = seasonAffinity;
        }
    }
}
