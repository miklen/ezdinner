using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using EzDinner.Core.DomainServices.RatingReminders;
using NodaTime;
using Xunit;

namespace EzDinner.UnitTests.RatingReminderTests;

public class RatingReminderSelectionTests
{
    private readonly Guid _familyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly LocalDate _today = new(2026, 10, 12);
    private readonly RatingReminderSelectionService _selection = new();

    private Dinner Planned(Dish dish, int offset)
    {
        var dinner = Dinner.CreateNew(_familyId, _today.PlusDays(offset));
        dinner.AddMenuItem(new(dish.Id));
        return dinner;
    }

    [Theory]
    [InlineData(-8, false)]
    [InlineData(-7, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void Eligibility_uses_inclusive_seven_day_past_window(int offset, bool eligible)
    {
        var dish = Dish.CreateNew(_familyId, "Lasagne");
        var result = _selection.Select(_familyId, [Planned(dish, offset)], [dish], RatingReminders.CreateNew(_userId), _today);
        Assert.Equal(eligible ? new[] { new RatingReminderOccurrenceValueObject(_familyId, dish.Id, _today.PlusDays(offset)) } : [], result);
    }

    [Fact]
    public void Another_members_rating_does_not_suppress_personal_reminder()
    {
        var dish = Dish.CreateNew(_familyId, "Tacos");
        var member = Guid.NewGuid();
        dish.SetRating(member, member, true, 4);
        Assert.Equal(dish.Id, Assert.Single(_selection.Select(_familyId, [Planned(dish, -1)], [dish], RatingReminders.CreateNew(_userId), _today)).DishId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3.5)]
    public void Any_own_rating_excludes_the_dish(double rating)
    {
        var dish = Dish.CreateNew(_familyId, "Tacos");
        dish.SetRating(_userId, _userId, true, rating);
        Assert.Empty(_selection.Select(_familyId, [Planned(dish, -1)], [dish], RatingReminders.CreateNew(_userId), _today));
    }

    [Fact]
    public void Archived_deleted_missing_and_other_family_dishes_are_excluded()
    {
        var archived = Dish.CreateNew(_familyId, "Archived");
        archived.Archive();
        var deleted = Dish.CreateNew(_familyId, "Deleted");
        deleted.Delete();
        var missing = Dish.CreateNew(_familyId, "Missing");
        var otherFamily = Dish.CreateNew(Guid.NewGuid(), "Elsewhere");
        Assert.Empty(_selection.Select(_familyId, [Planned(archived, -1), Planned(deleted, -1), Planned(missing, -1), Planned(otherFamily, -1)],
            [archived, deleted, otherFamily], RatingReminders.CreateNew(_userId), _today));
    }

    [Fact]
    public void Empty_opted_out_and_other_family_dinners_are_excluded()
    {
        var dish = Dish.CreateNew(_familyId, "Tacos");
        var optedOut = Planned(dish, -1);
        optedOut.SetOptOut("Out");
        var elsewhere = Dinner.CreateNew(Guid.NewGuid(), _today.PlusDays(-1));
        elsewhere.AddMenuItem(new(dish.Id));
        Assert.Empty(_selection.Select(_familyId, [optedOut, elsewhere, Dinner.CreateNew(_familyId, _today.PlusDays(-2))],
            [dish], RatingReminders.CreateNew(_userId), _today));
    }

    [Fact]
    public void Latest_occurrence_is_selected_before_dismissal()
    {
        var dish = Dish.CreateNew(_familyId, "Tacos");
        var state = RatingReminders.CreateNew(_userId);
        var dinners = new[] { Planned(dish, -4), Planned(dish, -1) };
        Assert.Equal(_today.PlusDays(-1), Assert.Single(_selection.Select(_familyId, dinners, [dish], state, _today)).DinnerDate);
        state.DismissThroughOccurrence(new(_familyId, dish.Id, _today.PlusDays(-1)), _today);
        Assert.Empty(_selection.Select(_familyId, dinners, [dish], state, _today));
    }

    [Fact]
    public void Ordering_is_newest_first_with_ordinal_identity_ties()
    {
        var first = new Dish(Guid.Parse("00000001-0000-0000-0000-000000000000"), _familyId, "A", null, [], "", false, []);
        var second = new Dish(Guid.Parse("00000002-0000-0000-0000-000000000000"), _familyId, "B", null, [], "", false, []);
        var older = Dish.CreateNew(_familyId, "Older");
        var dinners = new[] { Planned(older, -7), Planned(second, -1), Planned(first, -1) };
        var state = RatingReminders.CreateNew(_userId);
        Assert.Equal(new[] { first.Id, second.Id, older.Id }, _selection.Select(_familyId, dinners, [second, older, first], state, _today).Select(item => item.DishId));
        Assert.Equal(new[] { first.Id, second.Id, older.Id }, _selection.Select(_familyId, dinners.Reverse(), [first, older, second], state, _today).Select(item => item.DishId));
    }

    [Fact]
    public void Retroactive_replacement_has_independent_eligibility()
    {
        var original = Dish.CreateNew(_familyId, "Lasagne");
        var replacement = Dish.CreateNew(_familyId, "Tacos");
        var dinner = Planned(original, -1);
        var state = RatingReminders.CreateNew(_userId);
        state.DismissThroughOccurrence(new(_familyId, original.Id, dinner.Date), _today);
        dinner.ReplaceMenuItem(new(original.Id), new(replacement.Id));
        Assert.Equal(replacement.Id, Assert.Single(_selection.Select(_familyId, [dinner], [original, replacement], state, _today)).DishId);
        dinner.ReplaceMenuItem(new(replacement.Id), new(original.Id));
        Assert.Empty(_selection.Select(_familyId, [dinner], [original, replacement], state, _today));
    }

    [Fact]
    public void Push_attempt_does_not_remove_home_eligibility()
    {
        var dish = Dish.CreateNew(_familyId, "Tacos");
        var state = RatingReminders.CreateNew(_userId);
        state.SetPushEnabled(true);
        state.TryRecordPushAttempt(new(_familyId, dish.Id, _today.PlusDays(-1)), _today);
        Assert.Equal(dish.Id, Assert.Single(_selection.Select(_familyId, [Planned(dish, -1)], [dish], state, _today)).DishId);
    }
}
