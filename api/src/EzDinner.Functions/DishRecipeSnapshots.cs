using System.Text.Json;
using EzDinner.Application.Commands.RecipeSnapshots;
using EzDinner.Authorization.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web;

namespace EzDinner.Functions;

public sealed class DishRecipeSnapshots(IAuthzService authorization, PreviewRecipeSnapshotCommand preview,
    ConfirmRecipeSnapshotCommand confirm, RemoveRecipeSnapshotCommand remove)
{
    [Function("DishRecipePreview")]
    public Task<IActionResult> PreviewAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "dishes/{dishId}/recipe/preview/family/{familyId}")] HttpRequest request,
        string dishId, string familyId) => ExecuteAuthorizedAsync(request, dishId, familyId,
            async (family, dish) => new OkObjectResult(await preview.ExecuteAsync(family, dish, request.HttpContext.RequestAborted)));

    [Function("DishRecipeConfirm")]
    public Task<IActionResult> ConfirmAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dishes/{dishId}/recipe/family/{familyId}")] HttpRequest request,
        string dishId, string familyId) => ExecuteAuthorizedAsync(request, dishId, familyId, async (family, dish) =>
        {
            var candidate = await ReadCandidateAsync(request);
            await confirm.ExecuteAsync(family, dish, candidate, request.HttpContext.RequestAborted);
            return new NoContentResult();
        });

    [Function("DishRecipeRemove")]
    public Task<IActionResult> RemoveAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "dishes/{dishId}/recipe/family/{familyId}")] HttpRequest request,
        string dishId, string familyId) => ExecuteAuthorizedAsync(request, dishId, familyId, async (family, dish) =>
        {
            await remove.ExecuteAsync(family, dish);
            return new NoContentResult();
        });

    private async Task<IActionResult> ExecuteAuthorizedAsync(HttpRequest request, string dishId, string familyId,
        Func<Guid, Guid, Task<IActionResult>> operation)
    {
        if (request.HttpContext.User.Identity?.IsAuthenticated != true) return new UnauthorizedResult();
        if (!Guid.TryParse(familyId, out var family) || !Guid.TryParse(dishId, out var dish) || family == Guid.Empty || dish == Guid.Empty)
            return new BadRequestObjectResult("RECIPE_INVALID_REQUEST");
        var userId = request.HttpContext.User.GetNameIdentifierId();
        if (string.IsNullOrWhiteSpace(userId) || !authorization.Authorize(userId, family, Resources.Dish, Actions.Update))
            return new UnauthorizedResult();
        try { return await operation(family, dish); }
        catch (RecipeImportException exception) { return new BadRequestObjectResult(exception.Code); }
        catch (ArgumentException exception) { return new BadRequestObjectResult(exception.Message.Split('\n')[0].TrimEnd('\r')); }
        catch (JsonException) { return new BadRequestObjectResult("RECIPE_INVALID_REQUEST"); }
        catch (InvalidOperationException exception) when (exception.Message == "RECIPE_SOURCE_CHANGED")
        {
            return new ConflictObjectResult("RECIPE_SOURCE_CHANGED");
        }
        catch (KeyNotFoundException) { return new NotFoundObjectResult("DISH_NOT_FOUND"); }
        catch (UnauthorizedAccessException) { return new UnauthorizedResult(); }
    }

    private static async Task<RecipeCandidate> ReadCandidateAsync(HttpRequest request)
    {
        const int maximumBytes = 200_000;
        if (request.ContentLength > maximumBytes) throw new RecipeImportException("RECIPE_INVALID_REQUEST");
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await request.Body.ReadAsync(chunk, request.HttpContext.RequestAborted)) > 0)
        {
            if (buffer.Length + count > maximumBytes) throw new RecipeImportException("RECIPE_INVALID_REQUEST");
            buffer.Write(chunk, 0, count);
        }
        return JsonSerializer.Deserialize<RecipeCandidate>(buffer.ToArray(), new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 8
        }) ?? throw new RecipeImportException("RECIPE_INVALID_REQUEST");
    }
}
