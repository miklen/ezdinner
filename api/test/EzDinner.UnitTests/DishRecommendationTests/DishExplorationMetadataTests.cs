using System.Security.Claims;
using System.Text.Json;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.DishAggregate;
using EzDinner.Functions;
using EzDinner.Functions.Models.Query;
using EzDinner.Query.Core.DishQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using Xunit;

namespace EzDinner.UnitTests.DishRecommendationTests;

public class DishExplorationMetadataTests
{
    [Theory]
    [InlineData("?before=2026-10-03", 2026, 10, 2)]
    [InlineData("", 9999, 12, 31)]
    public async Task UsageStats_UseRequestedExclusiveBoundary_AndPreserveLegacyDefault(string query, int year, int month, int day)
    {
        var family = Guid.NewGuid();
        var service = new Mock<IDishQueryService>();
        service.Setup(x => x.GetDishUsageStatsAsync(family, LocalDate.MinIsoValue, It.IsAny<LocalDate>()))
            .ReturnsAsync(new Dictionary<Guid, DishStats>());
        var authorization = new Mock<IAuthzService>();
        authorization.Setup(x => x.Authorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "caller")], "test"));
        context.Request.QueryString = new QueryString(query);
        var function = new DishesGetStats(NullLogger<DishesGetStats>.Instance, service.Object, authorization.Object);

        Assert.IsType<OkObjectResult>(await function.Run(context.Request, family.ToString()));
        service.Verify(x => x.GetDishUsageStatsAsync(family, LocalDate.MinIsoValue, new LocalDate(year, month, day)), Times.Once);
    }

    [Fact]
    public void Catalog_ExposesRatingCount_WithoutChangingFivePointRating()
    {
        var dish = new Dish(Guid.NewGuid(), Guid.NewGuid(), "Favourite", null, [], "", false, [new Rating(Guid.NewGuid(), 10)]);
        var json = JsonSerializer.SerializeToElement(DishesQueryModel.FromDomain(dish));
        Assert.Equal(5, json.GetProperty("Rating").GetDouble());
        Assert.Equal(1, json.GetProperty("RatingCount").GetInt32());
    }
}
