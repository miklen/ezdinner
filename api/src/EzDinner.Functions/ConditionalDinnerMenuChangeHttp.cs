using EzDinner.Application.Commands.Dinners;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using NodaTime.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EzDinner.Functions;

public sealed class ConditionalDinnerMenuChangeHttp(ChangeDinnerMenuCommand changes,
    AddDishToDinnerCommand additions, IAuthzService authorization)
{
    public const string Header = "X-EzDinner-Mutation";
    public static bool IsRequested(HttpRequest request) => request.Headers[Header] == "conditional";

    public async Task<IActionResult> RunAsync(HttpRequest request, bool adding)
    {
        if (request.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
        try
        {
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var buffer = new char[16_001];
            var count = await reader.ReadBlockAsync(buffer.AsMemory(), request.HttpContext.RequestAborted);
            if (count > 16_000) return new BadRequestObjectResult("REQUEST_LIMIT_EXCEEDED");
            var payload = JsonSerializer.Deserialize<MutationPayload>(new string(buffer, 0, count), new JsonSerializerOptions
            { PropertyNameCaseInsensitive = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow });
            if (payload is null || !Guid.TryParse(payload.FamilyId, out var family) || family == Guid.Empty ||
                !Guid.TryParse(payload.DishId, out var dish) || dish == Guid.Empty || payload.Date is null ||
                payload.ExpectedState?.DishIds is null)
                return new BadRequestObjectResult("INVALID_PAYLOAD");
            var caller = request.HttpContext.User.GetNameIdentifierId();
            if (!Guid.TryParse(caller, out var planner) ||
                !authorization.Authorize(caller, family.ToString(), Resources.Dinner, Actions.Update) ||
                !authorization.Authorize(caller, family.ToString(), Resources.Dinner, Actions.Read)) return new UnauthorizedResult();
            var date = LocalDatePattern.Iso.Parse(payload.Date);
            if (!date.Success) return new BadRequestObjectResult("INVALID_DATE");
            var expected = new DinnerStateValueObject(payload.ExpectedState.DishIds, payload.ExpectedState.OptOutReason);
            var result = adding
                ? await additions.HandleConditionalAsync(family, date.Value, dish, planner, expected, request.HttpContext.RequestAborted)
                : await changes.RemoveAsync(family, date.Value, dish, expected, request.HttpContext.RequestAborted);
            return result switch
            {
                DinnerMenuChangeResult.Changed changed => new OkObjectResult(new
                {
                    outcome = "Changed",
                    before = new { dishIds = changed.Before.DishIds, optOutReason = changed.Before.OptOutReason,
                        changeId = changed.Before.ChangeId, dishChangeId = changed.Before.DishChangeId, optOutChangeId = changed.Before.OptOutChangeId },
                    after = new { dishIds = changed.After.DishIds, optOutReason = changed.After.OptOutReason,
                        changeId = changed.After.ChangeId, dishChangeId = changed.After.DishChangeId, optOutChangeId = changed.After.OptOutChangeId }
                }),
                DinnerMenuChangeResult.NoOp => new OkObjectResult(new { outcome = "NoOp" }),
                DinnerMenuChangeResult.Conflict => new ConflictObjectResult(new { outcome = "Conflict" }),
                _ => throw new InvalidOperationException("UNSUPPORTED_MENU_CHANGE_RESULT")
            };
        }
        catch (JsonException) { return new BadRequestObjectResult("INVALID_PAYLOAD"); }
        catch (ArgumentException exception) { return new BadRequestObjectResult(exception.Message); }
    }

    public sealed record StatePayload(Guid[]? DishIds, string? OptOutReason);
    public sealed record MutationPayload(string? FamilyId, string? Date, string? DishId, StatePayload? ExpectedState);
}
