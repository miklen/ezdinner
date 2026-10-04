using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using NodaTime;
using Xunit;

namespace EzDinner.UnitTests.RatingReminderTests;

public class RatingRemindersTests
{
    private readonly LocalDate _today = new(2026, 10, 12);
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _familyId = Guid.NewGuid();
    private readonly Guid _dishId = Guid.NewGuid();
    private RatingReminderOccurrenceValueObject Occurrence(int offset = -1) => new(_familyId, _dishId, _today.PlusDays(offset));

    [Fact]
    public void Push_is_off_when_state_is_new()
    {
        var state = RatingReminders.CreateNew(_userId);
        Assert.Equal(_userId, state.Id);
        Assert.False(state.PushEnabled);
        Assert.False(state.TryRecordPushAttempt(Occurrence(), _today));
        Assert.Empty(state.PushAttempts);
        Assert.Null(state.LatestPushAttemptDay);
    }

    [Fact]
    public void Dismissal_suppresses_earlier_occurrences_idempotently()
    {
        var state = RatingReminders.CreateNew(_userId);
        Assert.True(state.DismissThroughOccurrence(Occurrence(-2), _today));
        Assert.False(state.DismissThroughOccurrence(Occurrence(-2), _today));
        Assert.False(state.DismissThroughOccurrence(Occurrence(-4), _today));
        Assert.True(state.IsDismissed(Occurrence(-4)));
        Assert.Equal(_today.PlusDays(-2), Assert.Single(state.Dismissals).DismissedThroughDate);
    }

    [Fact]
    public void Later_occurrence_reappears_after_dismissal()
    {
        var state = RatingReminders.CreateNew(_userId);
        state.DismissThroughOccurrence(Occurrence(-2), _today);
        Assert.False(state.IsDismissed(Occurrence(-1)));
        Assert.True(state.DismissThroughOccurrence(Occurrence(-1), _today));
        Assert.Equal(_today.PlusDays(-1), Assert.Single(state.Dismissals).DismissedThroughDate);
    }

    [Fact]
    public void Dismissal_is_isolated_by_family_dish_and_user()
    {
        var state = RatingReminders.CreateNew(_userId);
        state.DismissThroughOccurrence(Occurrence(), _today);
        Assert.False(state.IsDismissed(new(Guid.NewGuid(), _dishId, _today.PlusDays(-1))));
        Assert.False(state.IsDismissed(new(_familyId, Guid.NewGuid(), _today.PlusDays(-1))));
        Assert.False(RatingReminders.CreateNew(Guid.NewGuid()).IsDismissed(Occurrence()));
    }

    [Theory]
    [InlineData(-8)]
    [InlineData(0)]
    [InlineData(1)]
    public void Invalid_dates_cannot_create_dismissals(int offset)
    {
        var state = RatingReminders.CreateNew(_userId);
        Assert.Throws<ArgumentException>(() => state.DismissThroughOccurrence(Occurrence(offset), _today));
        Assert.Empty(state.Dismissals);
    }

    [Fact]
    public void Daily_cap_is_global_across_families()
    {
        var state = RatingReminders.CreateNew(_userId);
        state.SetPushEnabled(true);
        Assert.True(state.TryRecordPushAttempt(Occurrence(), _today));
        var other = new RatingReminderOccurrenceValueObject(Guid.NewGuid(), Guid.NewGuid(), _today.PlusDays(-1));
        Assert.False(state.TryRecordPushAttempt(other, _today));
        Assert.Equal(Occurrence(), Assert.Single(state.PushAttempts).Occurrence);
        Assert.Equal(_today, state.LatestPushAttemptDay);
    }

    [Fact]
    public void Same_occurrence_cannot_be_attempted_on_another_day()
    {
        var state = RatingReminders.CreateNew(_userId);
        state.SetPushEnabled(true);
        Assert.True(state.TryRecordPushAttempt(Occurrence(), _today));
        Assert.False(state.TryRecordPushAttempt(Occurrence(), _today.PlusDays(1)));
        Assert.True(state.TryRecordPushAttempt(new(_familyId, _dishId, _today), _today.PlusDays(1)));
        Assert.Equal(2, state.PushAttempts.Count);
    }

    [Fact]
    public void Dismissed_or_expired_occurrences_cannot_be_attempted()
    {
        var state = RatingReminders.CreateNew(_userId);
        state.SetPushEnabled(true);
        state.DismissThroughOccurrence(Occurrence(), _today);
        Assert.False(state.TryRecordPushAttempt(Occurrence(), _today));
        Assert.False(state.TryRecordPushAttempt(Occurrence(-8), _today));
        Assert.Empty(state.PushAttempts);
    }

    [Fact]
    public void Pruning_retires_old_occurrences_and_preserves_today_cap()
    {
        var state = RatingReminders.CreateNew(_userId);
        state.SetPushEnabled(true);
        state.DismissThroughOccurrence(Occurrence(-7), _today);
        state.TryRecordPushAttempt(Occurrence(-6), _today);
        state.PruneExpiredOccurrences(_today.PlusDays(2));
        Assert.Empty(state.Dismissals);
        Assert.Empty(state.PushAttempts);
        Assert.Equal(_today, state.LatestPushAttemptDay);
        Assert.True(state.PushEnabled);
        var restored = RatingReminders.Hydrate(_userId, true, [], [], _today);
        Assert.False(restored.TryRecordPushAttempt(Occurrence(), _today));
    }

    [Fact]
    public void Boundary_occurrences_survive_pruning()
    {
        var state = RatingReminders.CreateNew(_userId);
        state.DismissThroughOccurrence(Occurrence(-7), _today);
        state.PruneExpiredOccurrences(_today);
        Assert.True(state.IsDismissed(Occurrence(-7)));
    }

    [Fact]
    public void Hydration_copies_collections_and_preserves_attempt_limits()
    {
        var attempts = new List<RatingReminderPushAttemptValueObject> { new(Occurrence(), _today) };
        var dismissals = new List<DismissedRatingReminderValueObject> { new(_familyId, _dishId, _today.PlusDays(-2)) };
        var state = RatingReminders.Hydrate(_userId, true, dismissals, attempts, null);
        attempts.Clear();
        dismissals.Clear();
        Assert.Single(state.PushAttempts);
        Assert.Single(state.Dismissals);
        Assert.Equal(_today, state.LatestPushAttemptDay);
        Assert.False(state.CanAttemptPush(new(Guid.NewGuid(), Guid.NewGuid(), _today.PlusDays(-1)), _today));
    }
}
