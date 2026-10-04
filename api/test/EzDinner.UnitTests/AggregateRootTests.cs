using EzDinner.Core.Aggregates;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace EzDinner.UnitTests
{
    public class AggregateRootTests
    {
        [Fact]
        public void Instance_IsInitialized_CanBeSerialized()
        {
            var root = new AggregateInstance();

            var json = JsonSerializer.Serialize(root);

            // no exception is good test
        }

        [Fact]
        public void Instance_IsInitialized_CanBeDeserialized()
        {
            var root = new AggregateInstance();
            var json = JsonSerializer.Serialize(root);

            var result = JsonSerializer.Deserialize<AggregateInstance>(json);

            Assert.Equal(root.Id, result.Id);
            // Verifies that the lambda property can be serialized and deserialized correctly
            Assert.Equal(root.PartitionKey, result.PartitionKey);
        }
    }

    public class AggregateInstance : AggregateRoot<Guid>
    {
        public AggregateInstance() : this(Guid.NewGuid())
        {
        }

        [JsonConstructor]
        public AggregateInstance(Guid id) : base(id)
        {
        }
    }
}
