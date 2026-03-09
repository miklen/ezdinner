using Casbin.Persist.Adapter.EFCore;
using Casbin.Persist.Adapter.EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq;

namespace EzDinner.Infrastructure
{
    public class CasbinEntityConfiguration : DefaultPersistPolicyEntityTypeConfiguration<string>
    {
        private const string _containerName = "CasbinRulesV2";

        public CasbinEntityConfiguration() : base(_containerName) {}

        public override void Configure(EntityTypeBuilder<EFCorePersistPolicy<string>> builder)
        {
            base.Configure(builder);
            builder.ToContainer(_containerName);
            builder.HasPartitionKey(p => p.Id);
            // EF Core Cosmos provider does not support index definitions - remove any added by base class
            foreach (var index in builder.Metadata.GetIndexes().ToList())
                builder.Metadata.RemoveIndex(index);
        }
    }
}
