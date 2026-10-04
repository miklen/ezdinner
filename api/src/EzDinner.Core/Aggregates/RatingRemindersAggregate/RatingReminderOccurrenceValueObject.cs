using NodaTime;

namespace EzDinner.Core.Aggregates.RatingRemindersAggregate;

public sealed record RatingReminderOccurrenceValueObject
{
    public Guid FamilyId { get; }
    public Guid DishId { get; }
    public LocalDate DinnerDate { get; }

    public RatingReminderOccurrenceValueObject(Guid familyId, Guid dishId, LocalDate dinnerDate)
    {
        if (familyId == Guid.Empty) throw new ArgumentException("RATING_REMINDER_FAMILY_REQUIRED", nameof(familyId));
        if (dishId == Guid.Empty) throw new ArgumentException("RATING_REMINDER_DISH_REQUIRED", nameof(dishId));
        FamilyId = familyId;
        DishId = dishId;
        DinnerDate = dinnerDate;
    }

    public bool IsRecentPast(LocalDate today) => DinnerDate >= today.PlusDays(-7) && DinnerDate < today;
}
