using NodaTime;

namespace EzDinner.Core.DomainServices.DishRecommendations;

public sealed class DishUsagePatternValueObject
{
    public IReadOnlyList<LocalDate> ServingDates { get; }
    public IReadOnlyList<LocalDate> OccurrenceStarts { get; }
    public int ServingCount => ServingDates.Count;
    public LocalDate? LastServed => ServingCount == 0 ? null : ServingDates[^1];
    public double? TypicalSpacingDays { get; }
    public bool NeverUsed => ServingCount == 0;

    public DishUsagePatternValueObject(IEnumerable<LocalDate> dates, LocalDate today)
    {
        var past = dates.Where(date => date < today).Distinct().Order().ToArray();
        ServingDates = Array.AsReadOnly(past);
        var starts = past.Where((date, index) => index == 0 || date != past[index - 1].PlusDays(1)).ToArray();
        OccurrenceStarts = Array.AsReadOnly(starts);
        if (starts.Length < 3) return;
        var gaps = starts.Skip(1).Select((date, index) => Period.Between(starts[index], date, PeriodUnits.Days).Days).Order().ToArray();
        var midpoint = gaps.Length / 2;
        TypicalSpacingDays = gaps.Length % 2 == 0 ? (gaps[midpoint - 1] + gaps[midpoint]) / 2d : gaps[midpoint];
    }

    public int? DaysSinceLastServing(LocalDate planningDate) => LastServed is LocalDate last
        ? Period.Between(last, planningDate, PeriodUnits.Days).Days
        : null;
}
