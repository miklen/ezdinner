using EzDinner.Application.Commands.Dinners;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web;
using NodaTime.Text;
using System.Text.Json;

namespace EzDinner.Functions;

public sealed class DinnerUndoMenuChange(UndoDinnerMenuChangeCommand command, IAuthzService authorization)
{
    [Function(nameof(DinnerUndoMenuChange))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families/{familyId}/dinners/{date}/undo-menu-change")] HttpRequest request,
        string familyId, string date)
    {
        if (request.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
        if (!Guid.TryParse(familyId, out var family) || family == Guid.Empty) return new BadRequestObjectResult("INVALID_FAMILY_ID");
        var caller = request.HttpContext.User.GetNameIdentifierId();
        if (caller is null || !authorization.Authorize(caller, familyId, Resources.Dinner, Actions.Update)) return new UnauthorizedResult();
        var parsedDate = LocalDatePattern.Iso.Parse(date);
        if (!parsedDate.Success) return new BadRequestObjectResult("INVALID_DATE");
        try
        {
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var buffer = new char[16_001];
            var count = await reader.ReadBlockAsync(buffer.AsMemory(), request.HttpContext.RequestAborted);
            if (count > 16_000) return new BadRequestObjectResult("REQUEST_LIMIT_EXCEEDED");
            var payload = JsonSerializer.Deserialize<UndoPayload>(new string(buffer, 0, count), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (payload is null || payload.DishId == Guid.Empty || payload.Before?.DishIds is null || payload.After?.DishIds is null)
                return new BadRequestObjectResult("INVALID_PAYLOAD");
            var before = new DinnerStateValueObject(payload.Before.DishIds, payload.Before.OptOutReason,
                payload.Before.ChangeId, payload.Before.DishChangeId, payload.Before.OptOutChangeId);
            var after = new DinnerStateValueObject(payload.After.DishIds, payload.After.OptOutReason,
                payload.After.ChangeId, payload.After.DishChangeId, payload.After.OptOutChangeId);
            var restored = await command.UndoAsync(family, parsedDate.Value, payload.DishId, before, after, request.HttpContext.RequestAborted);
            return restored ? new OkObjectResult(new { outcome = "Restored" }) : new ConflictObjectResult(new { outcome = "Conflict" });
        }
        catch (JsonException) { return new BadRequestObjectResult("INVALID_PAYLOAD"); }
        catch (ArgumentException exception) { return new BadRequestObjectResult(exception.Message); }
    }

    public sealed record StatePayload(Guid[]? DishIds, string? OptOutReason, Guid ChangeId = default, Guid DishChangeId = default, Guid OptOutChangeId = default);
    public sealed record UndoPayload(Guid DishId, StatePayload? Before, StatePayload? After);
}
