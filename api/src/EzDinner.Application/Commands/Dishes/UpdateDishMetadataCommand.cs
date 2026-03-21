using EzDinner.Core.Aggregates.DishAggregate;
using System;
using System.Collections.Generic;

namespace EzDinner.Application.Commands.Dishes
{
    public class UpdateDishMetadataCommand
    {
        public Guid FamilyId { get; }
        public Guid DishId { get; }
        public IReadOnlyList<DishRole>? Roles { get; }
        public EffortLevel? EffortLevel { get; }
        public SeasonAffinity? SeasonAffinity { get; }
        public string? Cuisine { get; }

        public UpdateDishMetadataCommand(
            Guid familyId,
            Guid dishId,
            IReadOnlyList<DishRole>? roles,
            EffortLevel? effortLevel,
            SeasonAffinity? seasonAffinity,
            string? cuisine)
        {
            FamilyId = familyId;
            DishId = dishId;
            Roles = roles;
            EffortLevel = effortLevel;
            SeasonAffinity = seasonAffinity;
            Cuisine = cuisine;
        }
    }
}
