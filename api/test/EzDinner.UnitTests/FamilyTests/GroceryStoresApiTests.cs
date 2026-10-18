using System.Security.Claims;
using System.Text;
using EzDinner.Application.Commands.Families;
using EzDinner.Authorization.Core;
using EzDinner.Core.Aggregates.FamilyAggregate;
using EzDinner.Functions;
using EzDinner.Functions.Models.Query;
using EzDinner.Query.Core.FamilyQueries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EzDinner.UnitTests.FamilyTests;

public class GroceryStoresApiTests
{
    private readonly Family _family = Family.CreateNew(Guid.NewGuid(), "Family");
    private readonly Mock<IFamilyRepository> _families = new();
    private readonly Mock<IGroceryStoreCatalog> _catalog = new();
    private readonly Mock<IAuthzService> _authorization = new();

    public GroceryStoresApiTests()
    {
        _families.Setup(repository => repository.GetFamily(_family.Id)).ReturnsAsync(_family);
        _catalog.Setup(catalog => catalog.GetStoresAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new GroceryStoreDescriptor("netto", "Netto", true, false, false),
            new GroceryStoreDescriptor("foetex", "Føtex", true, false, false),
            new GroceryStoreDescriptor("missing", "Missing", false, false, false)
        });
    }

    private SetGroceryStoresCommand Command() => new(_families.Object, _catalog.Object);
    private HttpRequest Request(string body = "{\"storeIds\":[\" NETTO \",\"foetex\",\"netto\"]}", Guid? userId = null)
    {
        var context = new DefaultHttpContext();
        if (userId.HasValue)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()!) }, "test"));
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context.Request;
    }

    private void Authorize(Guid owner) => _authorization.Setup(service => service.Authorize(owner.ToString(), _family.Id, Resources.Family, Actions.Update)).Returns(true);
    private Task<IActionResult> Run(HttpRequest request, string familyId = null) => new FamilySetGroceryStores(_authorization.Object, Command()).Run(request, familyId ?? _family.Id.ToString());

    [Fact]
    public async Task Any_owner_can_save_canonical_catalog_selection()
    {
        var secondOwner = Guid.NewGuid();
        _family.InviteFamilyMember(secondOwner);
        _family.SetMemberRole(secondOwner, true);
        foreach (var owner in new[] { _family.OwnerId, secondOwner })
        {
            Authorize(owner);
            var result = Assert.IsType<OkObjectResult>(await Run(Request(userId: owner)));
            Assert.Equal(new[] { "foetex", "netto" }, Assert.IsType<GroceryStoresQueryModel>(result.Value).GroceryStores);
        }
        _families.Verify(repository => repository.SaveGroceryStoresAsync(_family), Times.Exactly(2));
        _families.Verify(repository => repository.SaveAsync(It.IsAny<Family>()), Times.Never);
    }

    [Fact]
    public async Task Unauthenticated_members_and_other_family_users_cannot_read_or_write_selection()
    {
        var member = Guid.NewGuid();
        _family.InviteFamilyMember(member);
        Assert.IsType<UnauthorizedResult>(await Run(Request()));
        Assert.IsType<UnauthorizedResult>(await Run(Request(userId: member)));
        Authorize(_family.OwnerId);
        Assert.IsType<UnauthorizedResult>(await Run(Request(userId: _family.OwnerId), Guid.NewGuid().ToString()));
        _families.Verify(repository => repository.GetFamily(It.IsAny<Guid>()), Times.Never);
        _catalog.Verify(catalog => catalog.GetStoresAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{broken")]
    [InlineData("{\"storeIds\":null}")]
    [InlineData("{\"storeIds\":[null]}")]
    [InlineData("{\"storeIds\":[1]}")]
    [InlineData("{\"storeIds\":[\"unknown\"]}")]
    [InlineData("{\"storeIds\":[\"../netto\"]}")]
    [InlineData("{\"storeIds\":[],\"unexpected\":true}")]
    public async Task Invalid_input_does_not_save(string body)
    {
        Authorize(_family.OwnerId);
        Assert.IsType<BadRequestObjectResult>(await Run(Request(body, _family.OwnerId)));
        Assert.Empty(_family.GroceryStores);
        _families.Verify(repository => repository.SaveGroceryStoresAsync(It.IsAny<Family>()), Times.Never);
    }

    [Fact]
    public async Task Oversized_body_and_invalid_route_do_not_load_family()
    {
        Authorize(_family.OwnerId);
        Assert.IsType<BadRequestObjectResult>(await Run(Request(new string(' ', 16_385), _family.OwnerId)));
        Assert.IsType<BadRequestObjectResult>(await Run(Request(userId: _family.OwnerId), "invalid"));
        _families.Verify(repository => repository.GetFamily(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Missing_family_returns_not_found_without_catalog_or_save()
    {
        Authorize(_family.OwnerId);
        _families.Setup(repository => repository.GetFamily(_family.Id)).ReturnsAsync((Family)null);
        Assert.IsType<NotFoundObjectResult>(await Run(Request(userId: _family.OwnerId)));
        _catalog.Verify(catalog => catalog.GetStoresAsync(It.IsAny<CancellationToken>()), Times.Never);
        _families.Verify(repository => repository.SaveGroceryStoresAsync(It.IsAny<Family>()), Times.Never);
    }

    [Fact]
    public async Task Command_retains_missing_store_and_can_clear_selection()
    {
        _family.ReplaceGroceryStores(new[] { "removed-chain" }, new[] { "removed-chain" });
        Assert.Equal(new[] { "netto", "removed-chain" }, await Command().ExecuteAsync(_family.Id, new[] { "removed-chain", "netto" }));
        Assert.Empty(await Command().ExecuteAsync(_family.Id, Array.Empty<string>()));
        await Assert.ThrowsAsync<ArgumentException>(() => Command().ExecuteAsync(_family.Id, new[] { "removed-chain" }));
    }

    [Fact]
    public async Task Catalog_chain_can_be_selected_when_offer_source_is_unavailable()
    {
        Authorize(_family.OwnerId);
        var result = Assert.IsType<OkObjectResult>(await Run(Request("{\"storeIds\":[\"missing\"]}", _family.OwnerId)));
        Assert.Equal(new[] { "missing" }, Assert.IsType<GroceryStoresQueryModel>(result.Value).GroceryStores);
        _families.Verify(repository => repository.SaveGroceryStoresAsync(_family), Times.Once);
    }

    [Fact]
    public void Read_model_keeps_missing_selection_visible_as_unavailable()
    {
        var family = new FamilyDetails { GroceryStores = new[] { "missing" } };
        var result = new FamilyDetailsQueryModel(family, new[] { new GroceryStoreDescriptor("netto", "Netto", true, false, false) });
        Assert.Equal(new[] { "missing" }, result.GroceryStores);
        Assert.False(result.SupportedGroceryStores.Single(store => store.Id == "missing").IsAvailable);
        Assert.True(result.SupportedGroceryStores.Single(store => store.Id == "netto").IsAvailable);
    }
}
