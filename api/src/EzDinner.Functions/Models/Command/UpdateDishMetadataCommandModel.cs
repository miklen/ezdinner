using EzDinner.Core.Aggregates.DishAggregate;
using System.Collections.Generic;

namespace EzDinner.Functions.Models.Command
{
    public class UpdateDishMetadataCommandModel
    {
        public IReadOnlyList<DishRole>? Roles { get; set; }
        public EffortLevel? EffortLevel { get; set; }
        public SeasonAffinity? SeasonAffinity { get; set; }
        public string? Cuisine { get; set; }
    }
}
