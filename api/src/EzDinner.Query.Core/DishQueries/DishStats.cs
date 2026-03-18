using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;

namespace EzDinner.Query.Core.DishQueries
{
    public class DishStats
    {
        public Guid DishId { get; set; }
        public LocalDate LastUsed { get; set; }
        public int TimesUsed { get; set; }

        public DishStats(Guid dishId, IReadOnlyList<Dinner> dinners)
        {
            DishId = dishId;
            foreach (var dinner in dinners)
                AddDinner(dinner);
        }

        public DishStats()
        {
        }

        public void AddDinner(Dinner dinner)
        {
            foreach (var menu in dinner.Menu)
            {
                if (menu.DishId != DishId) continue;
                TimesUsed++;
                if (LastUsed < dinner.Date)
                    LastUsed = dinner.Date;
            }
        }
    }
}
