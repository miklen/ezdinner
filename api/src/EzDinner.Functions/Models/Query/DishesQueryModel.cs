using EzDinner.Core.Aggregates.DishAggregate;
using System;
using System.Collections.Generic;

namespace EzDinner.Functions.Models.Query
{
    /// <summary>
    /// Dish query model
    /// </summary>
    public class DishesQueryModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public double Rating { get; set; }
        public bool IsArchived { get; set; }
        public IReadOnlyList<DishRole>? Roles { get; set; }
        public bool RolesConfirmed { get; set; }
        public EffortLevel? EffortLevel { get; set; }
        public bool EffortLevelConfirmed { get; set; }
        public SeasonAffinity? SeasonAffinity { get; set; }
        public bool SeasonAffinityConfirmed { get; set; }
        public string? Cuisine { get; set; }
        public bool CuisineConfirmed { get; set; }

        public static DishesQueryModel FromDomain(Dish dish)
        {
            if (dish is null) throw new ArgumentNullException(nameof(dish));
            return new DishesQueryModel
            {
                Id = dish.Id,
                Name = dish.Name,
                Rating = dish.Rating / 2,
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
    }
}
