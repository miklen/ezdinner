using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Core.Aggregates.DishAggregate
{
    public interface IDishRepository
    {
        public Task SaveAsync(Dish dish);
        public Task<Dish?> GetDishAsync(Guid dishId);
        public Task<IEnumerable<Dish>> GetDishesAsync(Guid familyId);
    }
}
