using EzDinner.Core.Aggregates.DinnerAggregate;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.DishQueries
{
    public class DishQueryService : IDishQueryService
    {
        private readonly IDinnerRepository _dinnerRepository;

        public DishQueryService(IDinnerRepository dinnerRepository)
        {
            _dinnerRepository = dinnerRepository;
        }
        
        /// <summary>
        /// Gets usage statistics for dishes
        /// 
        /// TODO: Cache stats up front instead of calculating here
        /// </summary>
        /// <param name="familyId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<Dictionary<Guid, DishStats>> GetDishUsageStatsAsync(Guid familyId, LocalDate from, LocalDate to)
        {
            var dinners = _dinnerRepository.GetAsync(familyId, from, to);
            var dishes = new Dictionary<Guid, DishStats>();
            await foreach(var dinner in dinners)
            {
                foreach(var menu in dinner.Menu)
                {
                    if (!dishes.TryGetValue(menu.DishId, out var stats))
                        dishes.Add(menu.DishId, stats = new DishStats() { DishId = menu.DishId });
                    stats.AddDinner(dinner);
                }
            }
            return dishes;
        }
    }
}
