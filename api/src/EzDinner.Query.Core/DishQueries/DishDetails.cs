using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using System.Collections.Generic;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EzDinner.Query.Core.DishQueries
{
    /// <summary>
    /// Dish query model
    /// </summary>
    public class DishDetails
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }

        /// <summary>
        /// Rating on a scale between 0-5 with halfs allowed
        /// </summary>
        public double Rating { get; set; }

        /// <summary>
        /// Notes or recupe for the dish
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Recipe URL
        /// </summary>
        public string? Url { get; set; }

        public IEnumerable<DinnerDateQueryModel>? Dates { get; set; }

        public DishStats? DishStats { get; set; }

        public IEnumerable<RatingQueryModel>? Ratings { get; set; }

        public bool IsArchived { get; set; }

        public IReadOnlyList<DishRole>? Roles { get; set; }
        public bool RolesConfirmed { get; set; }
        public EffortLevel? EffortLevel { get; set; }
        public bool EffortLevelConfirmed { get; set; }
        public SeasonAffinity? SeasonAffinity { get; set; }
        public bool SeasonAffinityConfirmed { get; set; }
        public string? Cuisine { get; set; }
        public bool CuisineConfirmed { get; set; }

        public static DishDetails CreateNew(Dish dish, IReadOnlyList<Dinner> dinners)
        {
            if (dish is null) throw new ArgumentNullException(nameof(dish));
            return new DishDetails
            {
                Id = dish.Id,
                Name = dish.Name,
                Rating = dish.Rating / 2d,
                Ratings = dish.Ratings.Select(s => new RatingQueryModel(s.RaterId, s.RatingValue)),
                Url = dish.Url?.ToString() ?? "",
                Notes = dish.Notes ?? "",
                DishStats = new DishStats(dish.Id, dinners),
                Dates = CreateDinnersWithDaysBetween(dinners),
                IsArchived = dish.IsArchived,
                Roles = dish.Metadata.Roles,
                RolesConfirmed = dish.Metadata.RolesConfirmed,
                EffortLevel = dish.Metadata.EffortLevel,
                EffortLevelConfirmed = dish.Metadata.EffortLevelConfirmed,
                SeasonAffinity = dish.Metadata.SeasonAffinity,
                SeasonAffinityConfirmed = dish.Metadata.SeasonAffinityConfirmed,
                Cuisine = dish.Metadata.Cuisine,
                CuisineConfirmed = dish.Metadata.CuisineConfirmed
            };
        }

        private static IOrderedEnumerable<DinnerDateQueryModel> CreateDinnersWithDaysBetween(IReadOnlyList<Dinner> dinners)
        {
            return dinners.OrderBy(p => p.Date)
                                .Aggregate(new List<DinnerDateQueryModel>(), (acc, curr) =>
                                {
                                    acc.Add(new DinnerDateQueryModel() { Date = curr.Date, DaysSinceLast = Period.Between(acc.LastOrDefault()?.Date ?? curr.Date, curr.Date, PeriodUnits.Days).Days });
                                    return acc;
                                })
                                .OrderByDescending(p => p.Date);
        }
    }

    public class DinnerDateQueryModel
    {
        /// <summary>
        /// Date this dish was used as dinner
        /// </summary>
        public LocalDate Date { get; set; }
        /// <summary>
        /// Days since previous date. 0 if this is the first.
        /// </summary>
        public int DaysSinceLast { get; set; }
    }

    public class RatingQueryModel
    {
        public double Rating { get; set; }
        public Guid FamilyMemberId { get; }

        public RatingQueryModel(Guid familyMemberId, int rating)
        {
            FamilyMemberId = familyMemberId;
            Rating = rating / 2d;
        }
    }
}
