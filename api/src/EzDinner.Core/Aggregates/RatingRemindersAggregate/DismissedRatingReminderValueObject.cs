using NodaTime;

namespace EzDinner.Core.Aggregates.RatingRemindersAggregate;

public sealed record DismissedRatingReminderValueObject
{
    public Guid FamilyId { get; }
    public Guid DishId { get; }
    public LocalDate DismissedThroughDate { get; }

    public DismissedRatingReminderValueObject(Guid familyId, Guid dishId, LocalDate dismissedThroughDate)
    {
        var occurrence = new RatingReminderOccurrenceValueObject(familyId, dishId, dismissedThroughDate);
        FamilyId = occurrence.FamilyId;
        DishId = occurrence.DishId;
        DismissedThroughDate = occurrence.DinnerDate;
    }

    public bool Suppresses(RatingReminderOccurrenceValueObject occurrence) =>
        FamilyId == occurrence.FamilyId && DishId == occurrence.DishId && occurrence.DinnerDate <= DismissedThroughDate;
}
