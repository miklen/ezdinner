using EzDinner.Application.Commands.RatingReminders;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Query.Core.RatingReminderQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using NodaTime.Text;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EzDinner.Functions;

public sealed class RatingReminderFunctions(RatingReminderAccess access, GetRatingRemindersQuery reminders,
    GetRatingReminderPreferenceQuery preferences, DismissRatingReminderCommand dismissal,
    SetRatingReminderPreferenceCommand preference, IDishRepository dishes)
{
    private static readonly JsonSerializerOptions BodyOptions = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    [Function(nameof(GetRatingReminders))]
    public async Task<IActionResult> GetRatingReminders(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "families/{familyId}/rating-reminders")] HttpRequest request, string familyId)
    {
        var caller = RatingReminderAccess.Caller(request);
        if (!caller.HasValue) return new UnauthorizedResult();
        if (!Guid.TryParse(familyId, out var family) || family == Guid.Empty) return new BadRequestObjectResult("INVALID_FAMILY_ID");
        if (!await access.CanAccessAsync(caller.Value, family, Actions.Read, request.HttpContext.RequestAborted)) return new UnauthorizedResult();
        var result = await reminders.GetAsync(caller.Value, family, request.HttpContext.RequestAborted);
        return new OkObjectResult(new { today = LocalDatePattern.Iso.Format(result.Today), reminders = result.Reminders.Select(item =>
            new { dishId = item.DishId, dishName = item.DishName, dinnerDate = LocalDatePattern.Iso.Format(item.DinnerDate) }).ToList() });
    }

    [Function(nameof(DismissRatingReminder))]
    public async Task<IActionResult> DismissRatingReminder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "families/{familyId}/rating-reminders/{dishId}/dismiss")] HttpRequest request, string familyId, string dishId)
    {
        var caller = RatingReminderAccess.Caller(request);
        if (!caller.HasValue) return new UnauthorizedResult();
        if (!Guid.TryParse(familyId, out var family) || family == Guid.Empty || !Guid.TryParse(dishId, out var dish) || dish == Guid.Empty)
            return new BadRequestObjectResult("INVALID_ID");
        if (!await access.CanAccessAsync(caller.Value, family, Actions.Update, request.HttpContext.RequestAborted)) return new UnauthorizedResult();
        var target = await dishes.GetDishAsync(dish).WaitAsync(request.HttpContext.RequestAborted);
        if (target is null || target.FamilyId != family) return new BadRequestObjectResult("DISH_NOT_FOUND");
        try
        {
            var body = await JsonSerializer.DeserializeAsync<DismissBody>(request.Body, BodyOptions, request.HttpContext.RequestAborted);
            if (body?.DinnerDate is null || body.DinnerDate.Length != 10) return new BadRequestObjectResult("RATING_REMINDER_DATE_INVALID");
            var parsed = LocalDatePattern.Iso.Parse(body.DinnerDate);
            if (!parsed.Success) return new BadRequestObjectResult("RATING_REMINDER_DATE_INVALID");
            await dismissal.DismissAsync(caller.Value, new(family, dish, parsed.Value), request.HttpContext.RequestAborted);
            return new NoContentResult();
        }
        catch (JsonException) { return new BadRequestObjectResult("MISSING_VALUES"); }
        catch (ArgumentException exception) { return new BadRequestObjectResult(exception.Message); }
        catch (InvalidOperationException exception) when (exception.Message == "RATING_REMINDER_CONFLICT") { return new ConflictObjectResult(exception.Message); }
    }

    [Function(nameof(GetRatingReminderPreferences))]
    public async Task<IActionResult> GetRatingReminderPreferences(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rating-reminders/preferences")] HttpRequest request)
    {
        var caller = RatingReminderAccess.Caller(request);
        if (!caller.HasValue) return new UnauthorizedResult();
        return new OkObjectResult(new { pushEnabled = await preferences.GetAsync(caller.Value, request.HttpContext.RequestAborted) });
    }

    [Function(nameof(SetRatingReminderPreferences))]
    public async Task<IActionResult> SetRatingReminderPreferences(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "rating-reminders/preferences")] HttpRequest request)
    {
        var caller = RatingReminderAccess.Caller(request);
        if (!caller.HasValue) return new UnauthorizedResult();
        try
        {
            var body = await JsonSerializer.DeserializeAsync<PreferenceBody>(request.Body, BodyOptions, request.HttpContext.RequestAborted);
            if (body?.PushEnabled is null) return new BadRequestObjectResult("MISSING_VALUES");
            return new OkObjectResult(new { pushEnabled = await preference.SetAsync(caller.Value, body.PushEnabled.Value, request.HttpContext.RequestAborted) });
        }
        catch (JsonException) { return new BadRequestObjectResult("MISSING_VALUES"); }
        catch (InvalidOperationException exception) when (exception.Message == "RATING_REMINDER_CONFLICT") { return new ConflictObjectResult(exception.Message); }
    }

    public sealed record DismissBody(string? DinnerDate);
    public sealed record PreferenceBody(bool? PushEnabled);
}
