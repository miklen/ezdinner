using System;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public class DishScoreValueObject : IComparable<DishScoreValueObject>
    {
        public Guid DishId { get; }
        public string DishName { get; }
        public double TotalScore { get; }
        public double Rating { get; }
        public int DaysSinceLast { get; }

        public DishScoreValueObject(Guid dishId, string dishName, double totalScore, double rating, int daysSinceLast)
        {
            DishId = dishId;
            DishName = dishName;
            TotalScore = totalScore;
            Rating = rating;
            DaysSinceLast = daysSinceLast;
        }

        public int CompareTo(DishScoreValueObject? other)
        {
            if (other is null) return 1;
            return TotalScore.CompareTo(other.TotalScore);
        }
    }
}
