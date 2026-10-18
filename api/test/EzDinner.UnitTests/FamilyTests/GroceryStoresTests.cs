using EzDinner.Core.Aggregates.FamilyAggregate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace EzDinner.UnitTests.FamilyTests;

public class GroceryStoresTests
{
    [Fact]
    public void New_and_old_families_have_empty_selection()
    {
        var family = Family.CreateNew(Guid.NewGuid(), "Family");
        Assert.Empty(family.GroceryStores);
        var document = JObject.FromObject(family);
        document.Remove("GroceryStores");
        Assert.Empty(document.ToObject<Family>().GroceryStores);
    }

    [Fact]
    public void Replacement_is_canonical_immutable_and_round_trips()
    {
        var family = Family.CreateNew(Guid.NewGuid(), "Family");
        var stores = new[] { " NETTO ", "rema1000", "netto" };
        family.ReplaceGroceryStores(stores, new[] { "netto", "rema1000" });
        stores[0] = "changed";
        Assert.Equal(new[] { "netto", "rema1000" }, family.GroceryStores);
        Assert.Equal(family.GroceryStores, JsonConvert.DeserializeObject<Family>(JsonConvert.SerializeObject(family)).GroceryStores);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)family.GroceryStores)[0] = "changed");
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("../netto")]
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_selection_preserves_state_and_timestamp(string store)
    {
        var family = Family.CreateNew(Guid.NewGuid(), "Family");
        var timestamp = family.UpdatedDate;
        Assert.ThrowsAny<ArgumentException>(() => family.ReplaceGroceryStores(new[] { store }, new[] { "netto" }));
        Assert.Empty(family.GroceryStores);
        Assert.Equal(timestamp, family.UpdatedDate);
    }

    [Fact]
    public void Temporarily_missing_selected_store_can_be_retained_or_removed()
    {
        var family = Family.CreateNew(Guid.NewGuid(), "Family");
        family.ReplaceGroceryStores(new[] { "netto" }, new[] { "netto" });
        var timestamp = family.UpdatedDate;
        family.ReplaceGroceryStores(new[] { "netto" }, Array.Empty<string>());
        Assert.Equal(timestamp, family.UpdatedDate);
        family.ReplaceGroceryStores(Array.Empty<string>(), Array.Empty<string>());
        Assert.Empty(family.GroceryStores);
        Assert.True(family.UpdatedDate >= timestamp);
        Assert.Throws<ArgumentException>(() => family.ReplaceGroceryStores(new[] { "netto" }, Array.Empty<string>()));
    }

    [Fact]
    public void Changed_selection_advances_timestamp_without_changing_creation_date()
    {
        var previous = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var family = new Family(Guid.NewGuid(), "Family", new() { FamilyMember.CreateOwner(Guid.NewGuid()) }, previous, previous);
        family.ReplaceGroceryStores(new[] { "netto" }, new[] { "netto" });
        Assert.True(family.UpdatedDate > previous);
        Assert.Equal(previous, family.CreatedDate);
    }
}
