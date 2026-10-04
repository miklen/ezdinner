using NodaTime;

namespace EzDinner.Query.Core.RatingReminderQueries;

public sealed record RatingReminderResult(Guid DishId, string DishName, LocalDate DinnerDate);
public sealed record RatingReminderQueueResult(LocalDate Today, IReadOnlyList<RatingReminderResult> Reminders);
