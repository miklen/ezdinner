using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EzDinner.Query.Core.SuggestionQueries
{
    public class DishCandidateFactory
    {
        private readonly IDishRepository _dishRepository;
        private readonly IDinnerRepository _dinnerRepository;

        private const int DefaultDaysSinceLast = 365;
        private const double DefaultTypicalFrequencyDays = 14.0;

        public DishCandidateFactory(IDishRepository dishRepository, IDinnerRepository dinnerRepository)
        {
            _dishRepository = dishRepository;
            _dinnerRepository = dinnerRepository;
        }

        public async Task<IEnumerable<DishCandidate>> BuildCandidatesAsync(Guid familyId, LocalDate targetDate)
        {
            var dishes = await _dishRepository.GetDishesAsync(familyId);
            var allDinners = new List<Dinner>();
            await foreach (var dinner in _dinnerRepository.GetAsync(familyId, LocalDate.MinIsoValue, LocalDate.MaxIsoValue))
                allDinners.Add(dinner);

            return BuildCandidates(dishes, allDinners, targetDate);
        }

        public IEnumerable<DishCandidate> BuildCandidates(
            IEnumerable<Dish> dishes,
            IEnumerable<Dinner> allDinners,
            LocalDate targetDate)
        {
            var dinnerList = allDinners.ToList();
            return dishes
                .Where(d => !d.Deleted)
                .Select(d => BuildCandidate(d, dinnerList, targetDate));
        }

        private static DishCandidate BuildCandidate(Dish dish, List<Dinner> allDinners, LocalDate targetDate)
        {
            var usageDates = allDinners
                .Where(d => d.Menu.Any(m => m.DishId == dish.Id))
                .Select(d => d.Date)
                .OrderBy(d => d)
                .ToList();

            var daysSinceLast = usageDates.Count > 0
                ? Period.Between(usageDates[^1], targetDate, PeriodUnits.Days).Days
                : DefaultDaysSinceLast;

            var typicalFrequencyDays = ComputeTypicalFrequencyDays(usageDates);
            var leftoverFrequencyRatio = ComputeLeftoverFrequencyRatio(usageDates);

            return new DishCandidate(
                dish.Id,
                dish.Name,
                dish.Rating,
                daysSinceLast,
                typicalFrequencyDays,
                leftoverFrequencyRatio);
        }

        private static double ComputeTypicalFrequencyDays(List<LocalDate> sortedDates)
        {
            if (sortedDates.Count <= 1)
                return DefaultTypicalFrequencyDays;

            var totalDays = Period.Between(sortedDates[0], sortedDates[^1], PeriodUnits.Days).Days;
            return (double)totalDays / (sortedDates.Count - 1);
        }

        private static double ComputeLeftoverFrequencyRatio(List<LocalDate> sortedDates)
        {
            if (sortedDates.Count <= 1)
                return 0.0;

            var consecutivePairs = 0;
            for (var i = 0; i < sortedDates.Count - 1; i++)
            {
                if (Period.Between(sortedDates[i], sortedDates[i + 1], PeriodUnits.Days).Days == 1)
                    consecutivePairs++;
            }

            return (double)consecutivePairs / (sortedDates.Count - 1);
        }
    }
}
