using EzDinner.Core.Aggregates.DishAggregate;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.DishQueries
{
    public interface IDishQueryRepository
    {
        // TODO: Refactor to use Query Dish model instead of Command Dish model.
        public Task<IEnumerable<Dish>> GetDishesAsync(Guid familyId, bool includeArchived = false);
    }
}
