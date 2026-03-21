using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EzDinner.Core.DomainServices.DinnerSuggestions
{
    public static class DishCandidateFactory
    {
        private const int DefaultDaysSinceLast = 365;
        private const double DefaultTypicalFrequencyDays = 14.0;

        public static IEnumerable<DishCandidateValueObject> BuildCandidates(
            IEnumerable<Dish> dishes,
            IEnumerable<Dinner> allDinners,
            LocalDate targetDate)
        {
            var dinnerList = allDinners.ToList();
            return dishes
                .Where(d => !d.Deleted)
                .Where(d => d.Metadata.Roles.Count == 0 || d.Metadata.Roles.Contains(DishRole.Main)) // Count == 0 means not yet enriched — include unenriched dishes so they still surface in suggestions
                .Select(d => BuildCandidate(d, dinnerList, targetDate));
        }

        private static DishCandidateValueObject BuildCandidate(Dish dish, List<Dinner> allDinners, LocalDate targetDate)
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

            return new DishCandidateValueObject(
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
