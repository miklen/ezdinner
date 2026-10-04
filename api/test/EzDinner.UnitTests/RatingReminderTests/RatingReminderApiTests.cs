using EzDinner.Application.Commands.RatingReminders;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Core.Aggregates.RatingRemindersAggregate;
using EzDinner.Core.DomainServices.RatingReminders;
using EzDinner.Functions;
using EzDinner.Infrastructure.RatingReminders;
using EzDinner.Query.Core.RatingReminderQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EzDinner.UnitTests.RatingReminderTests;

public class RatingReminderApiTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Family _family;
    private readonly Dish _dish;
    private readonly Dinner _dinner;
    private readonly Mock<IDinnerRepository> _dinners = new();
    private readonly Mock<IDishRepository> _dishes = new();
    private readonly Mock<IFamilyRepository> _families = new();
    private readonly Mock<IAuthzService> _authorization = new();
    private readonly ReminderMemoryRepository _store = new();
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 10, 11, 22, 15));

    public RatingReminderApiTests()
    {
        _family = Family.CreateNew(_userId, "Family");
        _dish = Dish.CreateNew(_family.Id, "Lasagne");
        _dinner = Dinner.CreateNew(_family.Id, new(2026, 10, 11));
        _dinner.AddMenuItem(new(_dish.Id));
        _dishes.Setup(repository => repository.GetDishesAsync(_family.Id, false)).ReturnsAsync([_dish]);
        _dishes.Setup(repository => repository.GetDishAsync(_dish.Id)).ReturnsAsync(_dish);
        _dinners.Setup(repository => repository.GetAsync(_family.Id, new LocalDate(2026, 10, 5), new LocalDate(2026, 10, 11))).Returns(Stream([_dinner]));
        _dinners.Setup(repository => repository.GetAsync(_family.Id, _dinner.Date)).ReturnsAsync(_dinner);
        _families.Setup(repository => repository.GetFamily(_family.Id)).ReturnsAsync(_family);
        _authorization.Setup(service => service.Authorize(_userId, _family.Id, It.IsAny<string>(), It.IsAny<string>())).Returns(true);
    }

    private GetRatingRemindersQuery Query() => new(_dinners.Object, _dishes.Object, _store, new(), _clock);
    private DismissRatingReminderCommand Dismissal() => new(new(_store), _dinners.Object, _dishes.Object, _clock);
    private SetRatingReminderPreferenceCommand Preference() => new(new(_store), _clock);
    private RatingReminderFunctions Functions() => new(new(_authorization.Object, _families.Object), Query(), new(_store), Dismissal(), Preference(), _dishes.Object);

    private HttpRequest Request(string body = "", bool authenticated = true, Guid? user = null)
    {
        var context = new DefaultHttpContext();
        if (authenticated) context.User = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, (user ?? _userId).ToString())], "test"));
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context.Request;
    }

    [Fact]
    public async Task Query_uses_Copenhagen_calendar_day_without_writing_missing_state()
    {
        var result = await Query().GetAsync(_userId, _family.Id, default);
        Assert.Equal(new LocalDate(2026, 10, 12), result.Today);
        Assert.Equal(new RatingReminderResult(_dish.Id, "Lasagne", _dinner.Date), Assert.Single(result.Reminders));
        Assert.Equal(0, _store.Writes);
        _dishes.Verify(repository => repository.GetDishesAsync(_family.Id, false), Times.Once);
    }

    [Theory]
    [InlineData(2026, 3, 28, 23, 30, 29)]
    [InlineData(2026, 10, 24, 22, 30, 25)]
    public async Task Query_calendar_day_respects_DST(int year, int month, int day, int hour, int minute, int expectedDay)
    {
        _clock.Reset(Instant.FromUtc(year, month, day, hour, minute));
        var today = new LocalDate(year, month, expectedDay);
        _dinners.Setup(repository => repository.GetAsync(_family.Id, today.PlusDays(-7), today.PlusDays(-1))).Returns(Stream([]));
        var result = await Query().GetAsync(_userId, _family.Id, default);
        Assert.Equal(today, result.Today);
        Assert.Empty(result.Reminders);
        Assert.Equal(0, _store.Writes);
    }

    [Fact]
    public async Task Successful_dismissal_survives_reload_idempotently()
    {
        var occurrence = new RatingReminderOccurrenceValueObject(_family.Id, _dish.Id, _dinner.Date);
        await Dismissal().DismissAsync(_userId, occurrence, default);
        await Dismissal().DismissAsync(_userId, occurrence, default);
        Assert.Equal(1, _store.Writes);
        Assert.Empty((await Query().GetAsync(_userId, _family.Id, default)).Reminders);
    }

    [Fact]
    public async Task Removed_menu_occurrence_is_acknowledged_without_suppression()
    {
        _dinner.RemoveMenuItem(new(_dish.Id));
        await Dismissal().DismissAsync(_userId, new(_family.Id, _dish.Id, _dinner.Date), default);
        Assert.Equal(0, _store.Writes);
        Assert.Null(await _store.GetWithRevisionAsync(_userId, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-8)]
    public async Task Invalid_dismissal_dates_are_rejected_before_state_reads(int offset)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Dismissal().DismissAsync(_userId,
            new(_family.Id, _dish.Id, new LocalDate(2026, 10, 12).PlusDays(offset)), default));
        Assert.Equal(0, _store.Reads);
        Assert.Equal(0, _store.Writes);
    }

    [Fact]
    public async Task Concurrent_preference_write_is_preserved_when_dismissal_retries()
    {
        _store.BeforeNextSave = state => state.SetPushEnabled(true);
        await Dismissal().DismissAsync(_userId, new(_family.Id, _dish.Id, _dinner.Date), default);
        var state = (await _store.GetWithRevisionAsync(_userId, default)).Value.Reminders;
        Assert.True(state.PushEnabled);
        Assert.True(state.IsDismissed(new(_family.Id, _dish.Id, _dinner.Date)));
    }

    [Fact]
    public async Task Concurrent_dismissal_is_preserved_when_preference_retries()
    {
        _store.BeforeNextSave = state => state.DismissThroughOccurrence(new(_family.Id, _dish.Id, _dinner.Date), new(2026, 10, 12));
        Assert.True(await Preference().SetAsync(_userId, true, default));
        var state = (await _store.GetWithRevisionAsync(_userId, default)).Value.Reminders;
        Assert.True(state.PushEnabled);
        Assert.True(state.IsDismissed(new(_family.Id, _dish.Id, _dinner.Date)));
    }

    [Fact]
    public async Task Preference_defaults_off_without_a_write()
    {
        Assert.False(await new GetRatingReminderPreferenceQuery(_store).GetAsync(_userId, default));
        Assert.False(await Preference().SetAsync(_userId, false, default));
        Assert.Equal(0, _store.Writes);
    }

    [Fact]
    public async Task Conflict_retries_are_bounded()
    {
        _store.AlwaysConflict = true;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Preference().SetAsync(_userId, true, default));
        Assert.Equal("RATING_REMINDER_CONFLICT", exception.Message);
        Assert.Equal(4, _store.Reads);
    }

    [Fact]
    public async Task Unauthenticated_requests_are_rejected_without_state_access()
    {
        var functions = Functions();
        Assert.IsType<UnauthorizedResult>(await functions.GetRatingReminders(Request(authenticated: false), _family.Id.ToString()));
        Assert.IsType<UnauthorizedResult>(await functions.DismissRatingReminder(Request(authenticated: false), _family.Id.ToString(), _dish.Id.ToString()));
        Assert.IsType<UnauthorizedResult>(await functions.GetRatingReminderPreferences(Request(authenticated: false)));
        Assert.IsType<UnauthorizedResult>(await functions.SetRatingReminderPreferences(Request(authenticated: false)));
        Assert.Equal(0, _store.Reads);
    }

    [Fact]
    public async Task Cross_family_or_removed_members_cannot_read_reminder_state()
    {
        var functions = Functions();
        Assert.IsType<UnauthorizedResult>(await functions.GetRatingReminders(Request(), Guid.NewGuid().ToString()));
        _family.RemoveFamilyMember(_userId);
        Assert.IsType<UnauthorizedResult>(await functions.GetRatingReminders(Request(), _family.Id.ToString()));
        Assert.Equal(0, _store.Reads);
    }

    [Fact]
    public async Task Dish_from_another_family_cannot_be_dismissed()
    {
        var other = Dish.CreateNew(Guid.NewGuid(), "Other");
        _dishes.Setup(repository => repository.GetDishAsync(other.Id)).ReturnsAsync(other);
        Assert.IsType<BadRequestObjectResult>(await Functions().DismissRatingReminder(Request("{\"dinnerDate\":\"2026-10-11\"}"), _family.Id.ToString(), other.Id.ToString()));
        Assert.Equal(0, _store.Reads);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"pushEnabled\":\"true\"}")]
    [InlineData("{\"pushEnabled\":true,\"userId\":\"spoof\"}")]
    public async Task Invalid_or_spoofed_preferences_are_rejected_without_state_access(string body)
    {
        Assert.IsType<BadRequestObjectResult>(await Functions().SetRatingReminderPreferences(Request(body)));
        Assert.Equal(0, _store.Reads);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"dinnerDate\":\"2026-02-30\"}")]
    [InlineData("{\"dinnerDate\":\"2026-10-11\",\"userId\":\"spoof\"}")]
    public async Task Invalid_or_spoofed_dismissals_are_rejected_without_state_access(string body)
    {
        Assert.IsType<BadRequestObjectResult>(await Functions().DismissRatingReminder(Request(body), _family.Id.ToString(), _dish.Id.ToString()));
        Assert.Equal(0, _store.Reads);
    }

    [Fact]
    public async Task Response_fixtures_use_ISO_dates_and_confirmed_caller_preferences()
    {
        var functions = Functions();
        var response = Assert.IsType<OkObjectResult>(await functions.GetRatingReminders(Request(), _family.Id.ToString()));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response.Value));
        Assert.Equal("2026-10-12", json.RootElement.GetProperty("today").GetString());
        Assert.Equal("2026-10-11", json.RootElement.GetProperty("reminders")[0].GetProperty("dinnerDate").GetString());
        Assert.IsType<NoContentResult>(await functions.DismissRatingReminder(Request("{\"dinnerDate\":\"2026-10-11\"}"), _family.Id.ToString(), _dish.Id.ToString()));
        var saved = Assert.IsType<OkObjectResult>(await functions.SetRatingReminderPreferences(Request("{\"pushEnabled\":true}")));
        Assert.Equal("{\"pushEnabled\":true}", JsonSerializer.Serialize(saved.Value));
        Assert.Null(await _store.GetWithRevisionAsync(Guid.NewGuid(), default));
    }

    private static async IAsyncEnumerable<Dinner> Stream(IEnumerable<Dinner> dinners)
    {
        await Task.CompletedTask;
        foreach (var dinner in dinners) yield return dinner;
    }
}

public sealed class ReminderMemoryRepository : IRatingRemindersRepository
{
    private readonly Dictionary<Guid, (RatingRemindersDocument Document, int Revision)> _states = new();
    private readonly object _gate = new();
    public int Reads { get; private set; }
    public int Writes { get; private set; }
    public bool AlwaysConflict { get; set; }
    public Action<RatingReminders> BeforeNextSave { get; set; }
    public Action AfterNextSave { get; set; }

    private void NotifySaved()
    {
        var action = AfterNextSave;
        AfterNextSave = null;
        action?.Invoke();
    }

    public Task<(RatingReminders Reminders, string Revision)?> GetWithRevisionAsync(Guid userId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Reads++;
            if (!_states.TryGetValue(userId, out var stored)) return Task.FromResult<(RatingReminders, string)?>(null);
            return Task.FromResult<(RatingReminders, string)?>((stored.Document.ToAggregate(), stored.Revision.ToString()));
        }
    }

    private bool Intercept(Guid userId)
    {
        if (AlwaysConflict) return true;
        if (BeforeNextSave is null) return false;
        var state = _states.TryGetValue(userId, out var stored) ? stored.Document.ToAggregate() : RatingReminders.CreateNew(userId);
        BeforeNextSave(state);
        BeforeNextSave = null;
        _states[userId] = (RatingRemindersDocument.FromAggregate(state), stored.Revision + 1);
        return true;
    }

    public Task<bool> CreateIfAbsentAsync(RatingReminders reminders, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Intercept(reminders.Id) || _states.ContainsKey(reminders.Id)) return Task.FromResult(false);
            _states[reminders.Id] = (RatingRemindersDocument.FromAggregate(reminders), 1);
            Writes++;
            NotifySaved();
            return Task.FromResult(true);
        }
    }

    public Task<bool> SaveIfUnchangedAsync(RatingReminders reminders, string revision, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Intercept(reminders.Id) || !_states.TryGetValue(reminders.Id, out var stored) || stored.Revision.ToString() != revision) return Task.FromResult(false);
            _states[reminders.Id] = (RatingRemindersDocument.FromAggregate(reminders), stored.Revision + 1);
            Writes++;
            NotifySaved();
            return Task.FromResult(true);
        }
    }
}
