using EzDinner.Application.Commands.RatingReminders;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using EzDinner.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using NodaTime.Testing;
using Xunit;

namespace EzDinner.UnitTests.RatingReminderTests;

public class RatingReminderDeliveryTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Family _family;
    private readonly Dish _dish;
    private readonly Dish _older;
    private readonly Dinner _dinner;
    private readonly PushSubscription _subscription;
    private readonly Mock<IPushSubscriptionRepository> _subscriptions = new();
    private readonly Mock<IDinnerRepository> _dinners = new();
    private readonly Mock<IDishRepository> _dishes = new();
    private readonly Mock<IFamilyRepository> _families = new();
    private readonly Mock<IAuthzService> _authorization = new();
    private readonly Mock<IRatingReminderTransport> _transport = new();
    private readonly ReminderMemoryRepository _store = new();
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 10, 12, 13, 0));
    private readonly LocalDate _today = new(2026, 10, 12);

    public RatingReminderDeliveryTests()
    {
        _family = Family.CreateNew(_userId, "Family");
        _dish = Dish.CreateNew(_family.Id, "Lasagne");
        _older = Dish.CreateNew(_family.Id, "Tacos");
        _dinner = Dinner.CreateNew(_family.Id, _today.PlusDays(-1));
        _dinner.AddMenuItem(new(_dish.Id));
        var olderDinner = Dinner.CreateNew(_family.Id, _today.PlusDays(-2));
        olderDinner.AddMenuItem(new(_older.Id));
        _subscription = PushSubscription.CreateNew(_userId, _family.Id, "https://example.test/push", "key", "auth", "da");
        _subscriptions.Setup(repository => repository.GetAllFamilyIdsAsync()).ReturnsAsync([_family.Id]);
        _subscriptions.Setup(repository => repository.GetByFamilyIdAsync(_family.Id)).ReturnsAsync([_subscription]);
        _subscriptions.Setup(repository => repository.GetByUserIdAsync(_userId)).ReturnsAsync(_subscription);
        _dinners.Setup(repository => repository.GetAsync(_family.Id, _today.PlusDays(-7), _today.PlusDays(-1))).Returns(() => Stream([olderDinner, _dinner]));
        _dishes.Setup(repository => repository.GetDishesAsync(_family.Id, false)).ReturnsAsync([_older, _dish]);
        _families.Setup(repository => repository.GetFamily(_family.Id)).ReturnsAsync(_family);
        _authorization.Setup(service => service.Authorize(_userId, _family.Id, It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _transport.Setup(value => value.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<RatingReminderPushPayload>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(RatingReminderTransportOutcome.Sent);
    }

    private SendRatingRemindersCommand Command() => new(_subscriptions.Object, _dinners.Object, _dishes.Object, _store,
        new(), new(_families.Object, _authorization.Object), _transport.Object, _clock, NullLogger<SendRatingRemindersCommand>.Instance);
    private async Task Enable()
    {
        var state = RatingReminders.CreateNew(_userId);
        state.SetPushEnabled(true);
        await _store.CreateIfAbsentAsync(state, default);
    }
    private void VerifySends(int count) => _transport.Verify(value => value.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<RatingReminderPushPayload>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(count));

    [Fact]
    public async Task Most_recent_dish_has_localized_destination_and_bounded_expiry()
    {
        await Enable();
        await Command().SendAsync(default);
        _transport.Verify(value => value.SendAsync(_subscription, It.Is<RatingReminderPushPayload>(payload => payload.Type == "rating_reminder"
            && payload.DishName == "Lasagne" && payload.DinnerDate == "2026-10-11" && payload.Lang == "da"
            && payload.Destination == $"/dishes/{_dish.Id}?familyId={_family.Id}&ratingReminderDate=2026-10-11#my-rating"),
            It.Is<int>(ttl => ttl == (int)(_today.PlusDays(7).AtStartOfDayInZone(DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]).ToInstant() - _clock.GetCurrentInstant()).TotalSeconds), It.IsAny<CancellationToken>()), Times.Once);
        var state = (await _store.GetWithRevisionAsync(_userId, default)).Value.Reminders;
        Assert.Equal(_dish.Id, Assert.Single(state.PushAttempts).Occurrence.DishId);
        Assert.Equal(2, new EzDinner.Core.DomainServices.RatingReminders.RatingReminderSelectionService().Select(_family.Id,
            await Collect(_dinners.Object.GetAsync(_family.Id, _today.PlusDays(-7), _today.PlusDays(-1))), [_dish, _older], state, _today).Count);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("no-subscription")]
    [InlineData("membership")]
    [InlineData("access")]
    public async Task Ineligible_recipients_have_no_attempt(string condition)
    {
        if (condition != "off") await Enable();
        if (condition == "no-subscription") _subscriptions.Setup(repository => repository.GetByUserIdAsync(_userId)).ReturnsAsync((PushSubscription)null);
        if (condition == "membership") _family.RemoveFamilyMember(_userId);
        if (condition == "access") _authorization.Setup(service => service.Authorize(_userId, _family.Id, It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        await Command().SendAsync(default);
        VerifySends(0);
        Assert.Empty((await _store.GetWithRevisionAsync(_userId, default))?.Reminders.PushAttempts ?? []);
    }

    [Theory]
    [InlineData("rate")]
    [InlineData("dismiss")]
    [InlineData("plan")]
    [InlineData("archive")]
    [InlineData("off")]
    [InlineData("replacement")]
    [InlineData("family")]
    public async Task Final_revalidation_suppresses_changes_after_reservation(string change)
    {
        await Enable();
        _store.AfterNextSave = () =>
        {
            if (change == "rate") _dish.SetRating(_userId, _userId, true, 0);
            if (change == "plan") _dinner.ReplaceMenuItem(new(_dish.Id), new(_older.Id));
            if (change == "archive") _dish.Archive();
            if (change == "family") _family.RemoveFamilyMember(_userId);
            if (change == "replacement") _subscriptions.Setup(repository => repository.GetByUserIdAsync(_userId)).ReturnsAsync(PushSubscription.CreateNew(_userId, _family.Id, "https://example.test/new", "key", "auth", "en"));
            if (change is "off" or "dismiss")
            {
                var stored = _store.GetWithRevisionAsync(_userId, default).GetAwaiter().GetResult().Value;
                if (change == "off") stored.Reminders.SetPushEnabled(false);
                if (change == "dismiss") stored.Reminders.DismissThroughOccurrence(new(_family.Id, _dish.Id, _dinner.Date), _today);
                _store.SaveIfUnchangedAsync(stored.Reminders, stored.Revision, default).GetAwaiter().GetResult();
            }
        };
        await Command().SendAsync(default);
        VerifySends(0);
        Assert.Single((await _store.GetWithRevisionAsync(_userId, default)).Value.Reminders.PushAttempts);
    }

    [Fact]
    public async Task Rated_and_dismissed_dishes_are_suppressed_before_selection()
    {
        await Enable();
        _dish.SetRating(_userId, _userId, true, 0);
        var stored = (await _store.GetWithRevisionAsync(_userId, default)).Value;
        stored.Reminders.DismissThroughOccurrence(new(_family.Id, _older.Id, _today.PlusDays(-2)), _today);
        await _store.SaveIfUnchangedAsync(stored.Reminders, stored.Revision, default);
        await Command().SendAsync(default);
        VerifySends(0);
    }

    [Fact]
    public async Task Concurrent_duplicate_runs_have_one_transport_attempt()
    {
        await Enable();
        var dispatch = new TaskCompletionSource<RatingReminderTransportOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        _transport.Setup(value => value.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<RatingReminderPushPayload>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(dispatch.Task);
        var first = Command().SendAsync(default);
        await Command().SendAsync(default);
        VerifySends(1);
        dispatch.SetResult(RatingReminderTransportOutcome.Sent);
        await first;
        await Command().SendAsync(default);
        VerifySends(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Crash_or_ambiguous_failure_cannot_retry_reserved_occurrence(bool crash)
    {
        await Enable();
        if (crash) _store.AfterNextSave = () => throw new InvalidOperationException("Crash after reservation");
        else _transport.Setup(value => value.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<RatingReminderPushPayload>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException("Ambiguous"));
        await Command().SendAsync(default);
        await Command().SendAsync(default);
        VerifySends(crash ? 0 : 1);
        Assert.Single((await _store.GetWithRevisionAsync(_userId, default)).Value.Reminders.PushAttempts);
    }

    [Fact]
    public async Task Expired_subscription_is_removed()
    {
        await Enable();
        _transport.Setup(value => value.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<RatingReminderPushPayload>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(RatingReminderTransportOutcome.SubscriptionExpired);
        await Command().SendAsync(default);
        _subscriptions.Verify(repository => repository.DeleteAsync(_subscription), Times.Once);
    }

    [Fact]
    public async Task Independent_recipient_continues_after_a_failure()
    {
        await Enable();
        var otherUser = Guid.NewGuid();
        _family.InviteFamilyMember(otherUser);
        var other = PushSubscription.CreateNew(otherUser, _family.Id, "https://example.test/other", "key", "auth", "en");
        var state = RatingReminders.CreateNew(otherUser); state.SetPushEnabled(true); await _store.CreateIfAbsentAsync(state, default);
        _subscriptions.Setup(repository => repository.GetByFamilyIdAsync(_family.Id)).ReturnsAsync([_subscription, other]);
        _subscriptions.Setup(repository => repository.GetByUserIdAsync(otherUser)).ReturnsAsync(other);
        _authorization.Setup(service => service.Authorize(otherUser, _family.Id, It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _transport.Setup(value => value.SendAsync(_subscription, It.IsAny<RatingReminderPushPayload>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException());
        await Command().SendAsync(default);
        _transport.Verify(value => value.SendAsync(other, It.IsAny<RatingReminderPushPayload>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("wrong")]
    [InlineData("correct")]
    public async Task Shared_secret_is_checked_before_recipient_state(string secret)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["WebPush:SendTonightSecret"] = "correct" }).Build();
        var request = new DefaultHttpContext().Request;
        request.Headers["X-Push-Secret"] = secret;
        var result = await new PushSendRatingReminders(Command(), configuration).Run(request);
        if (secret == "correct") { Assert.IsType<OkResult>(result); _subscriptions.Verify(repository => repository.GetAllFamilyIdsAsync(), Times.Once); }
        else { Assert.IsType<UnauthorizedResult>(result); _subscriptions.VerifyNoOtherCalls(); }
        VerifySends(0);
    }

    private static async IAsyncEnumerable<Dinner> Stream(IEnumerable<Dinner> values) { await Task.CompletedTask; foreach (var value in values) yield return value; }
    private static async Task<List<Dinner>> Collect(IAsyncEnumerable<Dinner> values) { var result = new List<Dinner>(); await foreach (var value in values) result.Add(value); return result; }
}
