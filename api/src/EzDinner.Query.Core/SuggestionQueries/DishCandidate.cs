using System;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class DishCandidate
    {
        public Guid DishId { get; }
        public string DishName { get; }
        public double Rating { get; }
        public int DaysSinceLast { get; }
        public double TypicalFrequencyDays { get; }
        public double LeftoverFrequencyRatio { get; }

        public DishCandidate(
            Guid dishId,
            string dishName,
            double rating,
            int daysSinceLast,
            double typicalFrequencyDays,
            double leftoverFrequencyRatio)
        {
            DishId = dishId;
            DishName = dishName;
            Rating = rating;
            DaysSinceLast = daysSinceLast;
            TypicalFrequencyDays = typicalFrequencyDays;
            LeftoverFrequencyRatio = leftoverFrequencyRatio;
        }
    }
}
