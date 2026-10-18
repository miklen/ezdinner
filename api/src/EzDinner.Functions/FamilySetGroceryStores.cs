using System.Text.Json;
using System.Text.Json.Serialization;
using EzDinner.Application.Commands.Families;
using EzDinner.Authorization.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web;

namespace EzDinner.Functions;

public sealed class FamilySetGroceryStores(IAuthzService authorization, SetGroceryStoresCommand command)
{
    [Function(nameof(FamilySetGroceryStores))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "families/{familyId}/grocery-stores")] HttpRequest request, string familyId)
    {
        if (request.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
        if (!Guid.TryParse(familyId, out var family) || family == Guid.Empty) return new BadRequestObjectResult("INVALID_FAMILYID");
        var userId = request.HttpContext.User.GetNameIdentifierId();
        if (string.IsNullOrWhiteSpace(userId) || !authorization.Authorize(userId, family, Resources.Family, Actions.Update))
            return new UnauthorizedResult();
        try
        {
            var body = await ReadSelectionAsync(request);
            var stores = await command.ExecuteAsync(family, body.StoreIds, request.HttpContext.RequestAborted);
            return new OkObjectResult(new GroceryStoresQueryModel(stores));
        }
        catch (JsonException) { return new BadRequestObjectResult("GROCERY_STORES_INVALID"); }
        catch (ArgumentException exception) { return new BadRequestObjectResult(exception.Message.Split('\n')[0].TrimEnd('\r')); }
        catch (KeyNotFoundException) { return new NotFoundObjectResult("FAMILY_NOT_FOUND"); }
    }

    private static async Task<GroceryStoresCommandModel> ReadSelectionAsync(HttpRequest request)
    {
        const int maximumBytes = 16_384;
        if (request.ContentLength > maximumBytes) throw new JsonException();
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        int count;
        while ((count = await request.Body.ReadAsync(chunk, request.HttpContext.RequestAborted)) > 0)
        {
            if (buffer.Length + count > maximumBytes) throw new JsonException();
            buffer.Write(chunk, 0, count);
        }
        var body = JsonSerializer.Deserialize<GroceryStoresCommandModel>(buffer.ToArray(), new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 4
        });
        if (body?.StoreIds is null || body.StoreIds.Length > 100) throw new JsonException();
        return body;
    }
}

public sealed record GroceryStoresCommandModel(string[] StoreIds);
public sealed record GroceryStoresQueryModel(IReadOnlyList<string> GroceryStores);
