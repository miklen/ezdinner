using EzDinner.Core.Aggregates.DishAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EzDinner.Functions.Models.Query
{
    /// <summary>
    /// Dish query model
    /// </summary>
    public class DishesQueryModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }

        /// <summary>
        /// Rating on a scale between 0-5 with halfs allowed
        /// </summary>
        public double Rating { get; set; }

        public bool IsArchived { get; set; }

        // TODO: Switch to AutoMapper
        public static DishesQueryModel FromDomain(Dish dish)
        {
            if (dish is null) throw new ArgumentNullException(nameof(dish));
            return new DishesQueryModel { Id = dish.Id, Name = dish.Name, Rating = dish.Rating / 2, IsArchived = dish.IsArchived };
        }
    }
}
