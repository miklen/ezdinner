using EzDinner.Application.Commands.Dinners;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.PushSubscriptionAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using WebPush;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class ConditionalMenuChangeFunctionTests
{
    private readonly Guid family = Guid.NewGuid();
    private readonly Guid dish = Guid.NewGuid();
    private readonly LocalDate date = new(2026, 10, 5);
    private readonly Mock<IConditionalDinnerRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IWishlistRepository> wishes = new(MockBehavior.Strict);
    private readonly Mock<IAuthzService> authorization = new();

    [Theory]
    [InlineData("unauthenticated")]
    [InlineData("read")]
    [InlineData("update")]
    public async Task MissingPermissionNeverReadsOrWritesDinner(string denied)
    {
        authorization.Setup(service => service.Authorize(It.IsAny<string>(), family.ToString(), Resources.Dinner, It.IsAny<string>()))
            .Returns((string _, string _, string _, string action) => action != (denied == "read" ? Actions.Read : Actions.Update));
        Assert.IsType<UnauthorizedResult>(await Endpoint().RunAsync(Request(Payload(), denied != "unauthenticated"), true));
        repository.VerifyNoOtherCalls();
        wishes.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServerNoOpHasNoReceiptAndDoesNotGrantWishes(bool adding)
    {
        Permit();
        var dinner = Dinner.CreateNew(family, date);
        if (adding) dinner.AddMenuItem(new(dish));
        repository.Setup(store => store.GetWithRevisionAsync(family, date, default)).ReturnsAsync((dinner, "revision"));
        var result = Assert.IsType<OkObjectResult>(await Endpoint().RunAsync(Request(Payload()), adding));
        using var response = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal("NoOp", response.RootElement.GetProperty("outcome").GetString());
        Assert.False(response.RootElement.TryGetProperty("before", out _));
        wishes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SuccessfulAssignmentReturnsCanonicalReceiptThenGrantsWish()
    {
        Permit();
        var dinner = Dinner.CreateNew(family, date);
        repository.Setup(store => store.GetWithRevisionAsync(family, date, default)).ReturnsAsync((dinner, "revision"));
        repository.Setup(store => store.SaveIfUnchangedAsync(dinner, "revision", default)).ReturnsAsync(true);
        wishes.Setup(store => store.GetByDishAsync(family, dish)).ReturnsAsync((WishlistItem)null);
        var result = Assert.IsType<OkObjectResult>(await Endpoint().RunAsync(Request(Payload()), true));
        using var response = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal("Changed", response.RootElement.GetProperty("outcome").GetString());
        Assert.Empty(response.RootElement.GetProperty("before").GetProperty("dishIds").EnumerateArray());
        Assert.Equal(dish, response.RootElement.GetProperty("after").GetProperty("dishIds")[0].GetGuid());
        repository.VerifyAll();
        wishes.VerifyAll();
    }

    [Fact]
    public async Task ConflictingWriteHasNoReceiptOrWishSideEffect()
    {
        Permit();
        var dinner = Dinner.CreateNew(family, date);
        repository.Setup(store => store.GetWithRevisionAsync(family, date, default)).ReturnsAsync((dinner, "revision"));
        repository.Setup(store => store.SaveIfUnchangedAsync(dinner, "revision", default)).ReturnsAsync(false);
        var result = Assert.IsType<ConflictObjectResult>(await Endpoint().RunAsync(Request(Payload()), true));
        using var response = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal("Conflict", response.RootElement.GetProperty("outcome").GetString());
        Assert.False(response.RootElement.TryGetProperty("before", out _));
        wishes.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiptRoundTripOnlyUndoesOriginalAssignment(bool reassigned)
    {
        Permit();
        var dinner = Dinner.CreateNew(family, date);
        repository.Setup(store => store.GetWithRevisionAsync(family, date, default)).ReturnsAsync((dinner, "revision"));
        repository.Setup(store => store.SaveIfUnchangedAsync(dinner, "revision", default)).ReturnsAsync(true);
        wishes.Setup(store => store.GetByDishAsync(family, dish)).ReturnsAsync((WishlistItem)null);
        var changed = Assert.IsType<OkObjectResult>(await Endpoint().RunAsync(Request(Payload()), true));
        using var receipt = JsonDocument.Parse(JsonSerializer.Serialize(changed.Value));
        var after = receipt.RootElement.GetProperty("after");
        Assert.NotEqual(Guid.Empty, after.GetProperty("dishChangeId").GetGuid());
        if (reassigned) { dinner.RemoveMenuItem(new(dish)); dinner.AddMenuItem(new(dish)); }
        var undoPayload = JsonSerializer.Serialize(new { dishId = dish, before = receipt.RootElement.GetProperty("before"), after });
        var endpoint = new DinnerUndoMenuChange(new(repository.Object), authorization.Object);
        var result = await endpoint.Run(Request(undoPayload), family.ToString(), "2026-10-05");
        if (reassigned)
        {
            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(dish, Assert.Single(dinner.Menu).DishId);
        }
        else
        {
            Assert.IsType<OkObjectResult>(result);
            Assert.Empty(dinner.Menu);
        }
        repository.Verify(store => store.SaveIfUnchangedAsync(dinner, "revision", default), Times.Exactly(reassigned ? 1 : 2));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    public async Task InvalidPayloadNeverTouchesDinner(string payload)
    {
        Permit();
        Assert.IsType<BadRequestObjectResult>(await Endpoint().RunAsync(Request(payload), true));
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LegacyAddAndRemoveKeepEmptySuccessResponsesWithoutOptIn()
    {
        Permit();
        var dinner = Dinner.CreateNew(family, date);
        repository.Setup(store => store.GetWithRevisionAsync(family, date, default)).ReturnsAsync((dinner, "revision"));
        repository.Setup(store => store.SaveIfUnchangedAsync(dinner, "revision", default)).ReturnsAsync(true);
        wishes.Setup(store => store.GetByDishAsync(family, dish)).ReturnsAsync((WishlistItem)null);
        var add = new DinnerAddMenuItem(NullLogger<DinnerAddMenuItem>.Instance, Additions(), authorization.Object, Endpoint());
        Assert.IsType<OkResult>(await add.Run(Request(Payload())));
        var remove = new DinnerRemoveMenuItem(NullLogger<DinnerAddMenuItem>.Instance, new ChangeDinnerCommand(repository.Object), authorization.Object, Endpoint());
        Assert.IsType<OkResult>(await remove.Run(Request(Payload())));
        repository.Verify(store => store.SaveIfUnchangedAsync(dinner, "revision", default), Times.Exactly(2));
    }

    private ConditionalDinnerMenuChangeHttp Endpoint()
    {
        var changes = new ChangeDinnerMenuCommand(repository.Object);
        return new(changes, Additions(), authorization.Object);
    }

    private AddDishToDinnerCommand Additions() => new(new ChangeDinnerCommand(repository.Object), wishes.Object,
        Mock.Of<IWishStatsRepository>(), Mock.Of<IPushSubscriptionRepository>(), new WebPushClient(), NullLogger.Instance,
        new ChangeDinnerMenuCommand(repository.Object));

    private void Permit()
    {
        authorization.Setup(service => service.Authorize(It.IsAny<string>(), family.ToString(), Resources.Dinner, It.IsAny<string>())).Returns(true);
        authorization.Setup(service => service.Authorize(It.IsAny<string>(), family, Resources.Dinner, It.IsAny<string>())).Returns(true);
    }
    private string Payload() => JsonSerializer.Serialize(new { familyId = family, date = "2026-10-05", dishId = dish, expectedState = new { dishIds = Array.Empty<Guid>(), optOutReason = (string)null } });
    private static HttpRequest Request(string payload, bool authenticated = true)
    {
        var context = new DefaultHttpContext();
        if (authenticated) context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        return context.Request;
    }
}
