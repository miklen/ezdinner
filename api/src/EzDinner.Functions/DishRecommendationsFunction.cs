using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Query.Core.DishRecommendationQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using NodaTime.Text;
using System.Text.Json;

namespace EzDinner.Functions;

public sealed class DishRecommendationsFunction(DishRecommendationQuery query, IAuthzService authorization,
    DishRecommendationLimits limits, ILogger<DishRecommendationsFunction>? logger = null)
{
    public const int MaximumPayloadCharacters = 32_000;

    [Function(nameof(DishRecommendationsFunction))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "families/{familyId}/dish-recommendations")] HttpRequest request,
        string familyId)
    {
        if (request.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
        if (!Guid.TryParse(familyId, out var family) || family == Guid.Empty) return new BadRequestObjectResult("INVALID_FAMILY_ID");
        var caller = request.HttpContext.User.GetNameIdentifierId();
        if (caller is null || !authorization.Authorize(caller, familyId, Resources.Dish, Actions.Read) ||
            !authorization.Authorize(caller, familyId, Resources.Dinner, Actions.Read) ||
            !authorization.Authorize(caller, familyId, Resources.Wishlist, Actions.Read)) return new UnauthorizedResult();
        try
        {
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var buffer = new char[MaximumPayloadCharacters + 1];
            var length = await reader.ReadBlockAsync(buffer.AsMemory(), request.HttpContext.RequestAborted);
            if (length > MaximumPayloadCharacters) return new BadRequestObjectResult("REQUEST_LIMIT_EXCEEDED");
            var payload = JsonSerializer.Deserialize<RecommendationPayload>(new string(buffer, 0, length),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow });
            var intent = Parse(payload);
            limits.Validate(intent);
            var result = await query.RecommendAsync(family, intent, request.HttpContext.RequestAborted);
            return new OkObjectResult(new
            {
                outcome = result.Outcome.ToString(), result.ActiveConstraints, result.ContextSummary, result.Message,
                dishes = result.Dishes.Select(dish => new
                {
                    dish.DishId, dish.Name,
                    rating = dish.Signals.Rating, roles = dish.Signals.Roles.Select(role => role.ToString()),
                    dish.Signals.IsUnclassified, dish.Signals.IsWished, dish.Signals.WishVotes,
                    lastServed = dish.Signals.Usage.LastServed is {} date ? LocalDatePattern.Iso.Format(date) : null,
                    dish.Signals.Usage.ServingCount, dish.Signals.Usage.NeverUsed, dish.Signals.Usage.TypicalSpacingDays,
                    assignedDates = dish.Signals.AssignedDates.Select(LocalDatePattern.Iso.Format),
                    historicalReasons = dish.HistoricalReasons.Select(reason => new { kind = reason.Kind.ToString(), reason.Value }),
                    explanations = dish.Explanations.Select(reason => new { reason.Text, kind = reason.Kind.ToString(), reason.SourceReferences }),
                    dish.Limitations
                })
            });
        }
        catch (JsonException) { return new BadRequestObjectResult("INVALID_PAYLOAD"); }
        catch (ArgumentException exception) { return new BadRequestObjectResult(exception.Message); }
        catch (DishRecommendationProviderException exception)
        {
            int? providerHttpStatus = null;
            JsonException? parsingFailure = null;
            for (Exception? cause = exception.InnerException; cause is not null; cause = cause.InnerException)
            {
                if (cause is JsonException jsonException) parsingFailure = jsonException;
                if (cause is HttpRequestException { StatusCode: {} status })
                {
                    providerHttpStatus = (int)status;
                    break;
                }
            }
            var diagnosticCode = exception.Message switch
            {
                "PROVIDER_TIMEOUT" or "PROVIDER_UNAVAILABLE" or "MALFORMED_PROVIDER_RESPONSE" or
                "UNSUPPORTED_PROVIDER_MATCH" or "INVALID_PROVIDER_ROLE" or "UNSUPPORTED_PROVIDER_EVIDENCE"
                    => exception.Message,
                _ => "UNKNOWN_PROVIDER_FAILURE"
            };
            logger?.LogWarning("Dish recommendation provider failed. Code: {ProviderCode}; inner type: {InnerExceptionType}; HTTP status: {ProviderHttpStatus}",
                diagnosticCode, exception.InnerException?.GetType().FullName, providerHttpStatus);
            if (parsingFailure is not null)
                logger?.LogWarning("Dish recommendation provider JSON failed. Path: {JsonPath}; line: {JsonLine}; byte position: {JsonBytePosition}",
                    parsingFailure.Path, parsingFailure.LineNumber, parsingFailure.BytePositionInLine);
            return new ObjectResult(new { code = exception.Message }) { StatusCode = exception.Message == "PROVIDER_TIMEOUT" ? 504 : 502 };
        }
    }

    private static DishRecommendationRequest Parse(RecommendationPayload? payload)
    {
        if (payload is null || payload.SelectedMonday is null || payload.Locale is null || payload.Mode is null ||
            payload.Turns is null || payload.Constraints is null || payload.ExcludedDishIds is null)
            throw new ArgumentException("INVALID_PAYLOAD");
        var monday = LocalDatePattern.Iso.Parse(payload.SelectedMonday);
        if (!monday.Success) throw new ArgumentException("INVALID_SELECTED_MONDAY");
        NodaTime.LocalDate? target = null;
        if (payload.TargetDate is not null)
        {
            var date = LocalDatePattern.Iso.Parse(payload.TargetDate);
            if (!date.Success) throw new ArgumentException("INVALID_TARGET_DATE");
            target = date.Value;
        }
        if (!Enum.TryParse<RecommendationMode>(payload.Mode, true, out var mode) || !Enum.IsDefined(mode)) throw new ArgumentException("INVALID_MODE");
        DishRole? role = null;
        if (payload.Role is not null)
        {
            if (!Enum.TryParse<DishRole>(payload.Role, true, out var parsedRole) || !Enum.IsDefined(parsedRole)) throw new ArgumentException("INVALID_ROLE");
            role = parsedRole;
        }
        var exclusions = new HashSet<Guid>();
        foreach (var id in payload.ExcludedDishIds)
        {
            if (!Guid.TryParse(id, out var excluded) || excluded == Guid.Empty) throw new ArgumentException("INVALID_EXCLUSION");
            exclusions.Add(excluded);
        }
        return new(monday.Value, target, mode, payload.Locale, payload.Turns, payload.Constraints, exclusions, role, payload.NameFilter ?? "");
    }

    public sealed record RecommendationPayload(string? SelectedMonday, string? TargetDate, string? Mode,
        string? Locale, string[]? Turns, string[]? Constraints, string[]? ExcludedDishIds, string? Role, string? NameFilter = null);
}
