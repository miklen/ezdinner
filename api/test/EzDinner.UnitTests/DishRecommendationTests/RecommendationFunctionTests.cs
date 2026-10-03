using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DinnerAggregate;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Core.Aggregates.WishlistAggregate;
using EzDinner.Core.DomainServices.DishRecommendations;
using EzDinner.Functions;
using EzDinner.Query.Core.DishRecommendationQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NodaTime;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class RecommendationFunctionTests
{
    [Theory]
    [InlineData("unauthenticated")]
    [InlineData("dish")]
    [InlineData("dinner")]
    [InlineData("wishlist")]
    public async Task UnauthorizedFamilyAccessNeverLoadsEvidence(string denied)
    {
        var authorization = new Mock<IAuthzService>();
        authorization.Setup(service => service.Authorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), Actions.Read))
            .Returns((string _, string _, string resource, string _) => resource != Resource(denied));
        var dishes = new Mock<IDishRepository>(MockBehavior.Strict);
        var endpoint = Endpoint(authorization.Object, dishes.Object);
        var request = Request("{}", denied != "unauthenticated");
        Assert.IsType<UnauthorizedResult>(await endpoint.Run(request, Guid.NewGuid().ToString()));
        dishes.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"selectedMonday\":\"invalid\",\"locale\":\"en\",\"mode\":\"automatic\",\"turns\":[],\"constraints\":[],\"excludedDishIds\":[]}")]
    [InlineData("{\"selectedMonday\":\"2026-10-05\",\"locale\":\"fr\",\"mode\":\"automatic\",\"turns\":[],\"constraints\":[],\"excludedDishIds\":[]}")]
    [InlineData("{\"selectedMonday\":\"2026-10-05\",\"locale\":\"en\",\"mode\":\"automatic\",\"turns\":[],\"constraints\":[],\"excludedDishIds\":[\"invalid\"]}")]
    public async Task InvalidPayloadNeverLoadsEvidence(string payload)
    {
        var authorization = new Mock<IAuthzService>();
        authorization.Setup(service => service.Authorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), Actions.Read)).Returns(true);
        var dishes = new Mock<IDishRepository>(MockBehavior.Strict);
        Assert.IsType<BadRequestObjectResult>(await Endpoint(authorization.Object, dishes.Object).Run(Request(payload, true), Guid.NewGuid().ToString()));
        dishes.VerifyNoOtherCalls();
    }

    private static DishRecommendationsFunction Endpoint(IAuthzService authorization, IDishRepository dishes)
    {
        var limits = new DishRecommendationLimits();
        var rule = new ResurfacingRule();
        var assembler = new DishRecommendationContextAssembler(dishes, Mock.Of<IDinnerRepository>(), Mock.Of<IWishlistRepository>(), SystemClock.Instance);
        return new(new(assembler, new(rule), rule, Mock.Of<IDishRecommendationLlmClient>(), limits), authorization, limits);
    }

    private static HttpRequest Request(string payload, bool authenticated)
    {
        var context = new DefaultHttpContext();
        if (authenticated) context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        return context.Request;
    }

    private static string Resource(string denied) => denied switch
    {
        "dish" => Resources.Dish,
        "dinner" => Resources.Dinner,
        "wishlist" => Resources.Wishlist,
        _ => "none"
    };
}
